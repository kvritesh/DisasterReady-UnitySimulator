using UnityEngine;
using UnityEngine.UI;
using DisasterReady.Terrain;
using DisasterReady.Utility;

namespace DisasterReady.Emergency
{
    /// <summary>
    /// DEMO SAFE-DIRECTION HEURISTIC.
    ///
    /// While active, periodically samples a ring of fixed candidate directions
    /// around this transform (the player) using TerrainService - the same
    /// ITerrainDataProvider abstraction the terrain-info panel and Early Warning
    /// heuristics already use - and rotates a simple in-world arrow to point at
    /// the best-scoring candidate.
    ///
    /// Each candidate is scored on two things: how well it's aligned with the
    /// direction toward <see cref="Objective"/> (the actual emergency
    /// destination, e.g. the HighestPointMarker) and, secondarily, local terrain
    /// quality (elevation gain with a small slope penalty). Objective alignment
    /// is weighted far more heavily than terrain quality - see
    /// <see cref="ObjectiveWeight"/> vs <see cref="TerrainQualityWeight"/> -
    /// specifically so the arrow reliably leads toward the actual destination
    /// instead of wandering off toward an unrelated nearby high point; terrain
    /// quality only breaks ties between directions that are similarly aligned
    /// with the objective (e.g. picking the gentler of two roughly-equal paths).
    /// With no Objective assigned this falls back to the original pure local
    /// hill-climb behaviour.
    ///
    /// This is a deliberately simple, fully deterministic heuristic for this
    /// scripted demo: no randomness, no pathfinding, no claim of validated
    /// evacuation guidance. It never states a direction is "guaranteed safe" -
    /// only that it currently looks like the way toward higher ground, which is
    /// exactly what it computes.
    /// </summary>
    public class SafeDirectionGuide : MonoBehaviour
    {
        [Header("Objective")]
        [Tooltip("The actual emergency destination (e.g. HighestPointMarker). Assigned by SceneBuilder. Guidance falls back to a pure local terrain hill-climb if left unassigned.")]
        public Transform Objective;

        [Header("Sampling")]
        public int CandidateDirections = 8;
        public float SampleDistance = 8f;
        public float UpdateInterval = 0.5f;
        [Tooltip("How strongly a candidate's own slope is penalised, per degree.")]
        public float SlopePenaltyPerDegree = 0.02f;
        [Tooltip("Weight on how well a candidate direction is aligned with the direction toward Objective. Deliberately much larger than TerrainQualityWeight so the arrow reliably leads to the objective rather than an unrelated local bump.")]
        public float ObjectiveWeight = 12f;
        [Tooltip("Weight on local terrain quality (elevation gain minus slope penalty). Only meant to break ties between directions that are similarly aligned with the objective.")]
        public float TerrainQualityWeight = 1f;

        [Header("UI")]
        public Text StatusText;
        [Tooltip("Optional backing plate for StatusText (set by SceneBuilder). When assigned, this whole plate is shown/hidden instead of just the text, so the HUD readout gets a readable backing panel. Falls back to toggling StatusText directly if left unassigned.")]
        public GameObject StatusPanel;

        private Transform _arrowPivot;
        private Transform _arrowHead;
        private Transform _arrowHalo;
        private Material _arrowMat;
        private static readonly Color ArrowColor = new Color(0.95f, 0.78f, 0.2f);
        private float _timer;
        private float _pulseT;
        private float _currentYaw;
        private float _targetYaw;
        private bool _active;
        private bool _hasHeading;

        private void Awake()
        {
            BuildArrowVisual();
            SetArrowVisible(false);
        }

        public void Activate()
        {
            _active = true;
            SetArrowVisible(true);
            if (StatusPanel != null) StatusPanel.SetActive(true);
            else if (StatusText != null) StatusText.gameObject.SetActive(true);
            if (StatusText != null)
            {
                StatusText.text = "DEMO SAFE-DIRECTION HEURISTIC\nMove toward higher terrain";
            }
            // Force an immediate recompute instead of waiting a full UpdateInterval.
            _timer = UpdateInterval;
            RecomputeDirection();
        }

        public void Deactivate()
        {
            _active = false;
            SetArrowVisible(false);
            if (StatusPanel != null) StatusPanel.SetActive(false);
            else if (StatusText != null) StatusText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_active) return;

            _timer += Time.deltaTime;
            if (_timer >= UpdateInterval)
            {
                _timer = 0f;
                RecomputeDirection();
            }

            if (_hasHeading && _arrowPivot != null)
            {
                _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, Time.deltaTime * 6f);
                _arrowPivot.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
            }

            // Presentation-only pulse so the arrow reads clearly against any
            // terrain colour and stays noticeable out of the corner of the
            // player's eye while moving - does not affect heading/scoring.
            _pulseT += Time.deltaTime;
            float pulse01 = 0.5f + 0.5f * Mathf.Sin(_pulseT * 2.6f);
            if (_arrowHead != null)
            {
                float scale = Mathf.Lerp(0.92f, 1.18f, pulse01);
                _arrowHead.localScale = Vector3.one * scale;
            }
            if (_arrowMat != null && _arrowMat.HasProperty("_EmissionColor"))
            {
                float glow = Mathf.Lerp(1.4f, 2.6f, pulse01);
                _arrowMat.SetColor("_EmissionColor", ArrowColor * glow);
            }
            if (_arrowHalo != null)
            {
                float haloScale = Mathf.Lerp(0.85f, 1.25f, pulse01);
                _arrowHalo.localScale = new Vector3(haloScale, 1f, haloScale);
            }
        }

        /// <summary>
        /// Evaluates CandidateDirections evenly-spaced compass headings around the
        /// player, each sampled SampleDistance meters out via TerrainService, and
        /// keeps whichever scores best. Score combines (a) how well the candidate
        /// is aligned with the direction toward Objective - dot(candidateDir,
        /// directionToObjective), weighted by ObjectiveWeight - and (b) local
        /// terrain quality (elevation gain minus a slope penalty), weighted by
        /// the much smaller TerrainQualityWeight. Simple greedy scoring, no
        /// randomness - same inputs always produce same output.
        /// </summary>
        private void RecomputeDirection()
        {
            Vector3 origin = transform.position;

            bool hasObjective = false;
            Vector3 towardObjective = Vector3.zero;
            if (Objective != null)
            {
                Vector3 toObjective = Objective.position - origin;
                toObjective.y = 0f;
                if (toObjective.sqrMagnitude > 0.0001f)
                {
                    towardObjective = toObjective.normalized;
                    hasObjective = true;
                }
            }

            float bestScore = 0f;
            Vector3 bestDir = transform.forward;
            bool found = false;

            int candidateCount = Mathf.Max(1, CandidateDirections);
            for (int i = 0; i < candidateCount; i++)
            {
                float angleDeg = (360f / candidateCount) * i;
                Vector3 dir = Quaternion.Euler(0f, angleDeg, 0f) * Vector3.forward;
                Vector3 samplePos = origin + dir * SampleDistance;

                float elevationGain = 0f;
                float terrainQuality = 0f;

                if (TerrainService.TryGetSample(samplePos, out var sample) && sample.IsValid)
                {
                    elevationGain = sample.ElevationMeters - origin.y;
                    terrainQuality = elevationGain - sample.SlopeDegrees * SlopePenaltyPerDegree;
                }
                else
                {
                    // Fallback for mesh-based terrain environments (Synty, modular tiles)
                    if (Physics.Raycast(samplePos + Vector3.up * 50f, Vector3.down, out RaycastHit groundHit, 100f, ~LayerMask.GetMask("UI", "Ignore Raycast"), QueryTriggerInteraction.Ignore))
                    {
                        elevationGain = groundHit.point.y - origin.y;
                        float slope = Vector3.Angle(groundHit.normal, Vector3.up);
                        terrainQuality = elevationGain - slope * SlopePenaltyPerDegree;
                    }
                    else if (hasObjective)
                    {
                        terrainQuality = 0f;
                    }
                    else
                    {
                        continue;
                    }
                }

                float objectiveAlignment = hasObjective ? Vector3.Dot(dir, towardObjective) : 0f;
                float score = objectiveAlignment * ObjectiveWeight + terrainQuality * TerrainQualityWeight;

                if (!found || score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                    found = true;
                }
            }

            if (found)
            {
                _targetYaw = Mathf.Atan2(bestDir.x, bestDir.z) * Mathf.Rad2Deg;
                _hasHeading = true;
            }
        }

        // -----------------------------------------------------------------
        // Simple runtime-built arrow (cone + shaft), reusing ProceduralMeshFactory
        // so this stays visually consistent with the rest of the demo's low-poly
        // look without needing any imported model/sprite asset.
        // -----------------------------------------------------------------
        private void BuildArrowVisual()
        {
            var pivotGO = new GameObject("SafeDirectionArrow");
            pivotGO.transform.SetParent(transform, false);
            // Slightly higher than before (2.6 -> 3.1) so it clears head-height
            // foliage/roofs more reliably and reads against open sky rather than
            // blending into nearby terrain detail.
            pivotGO.transform.localPosition = new Vector3(0f, 3.1f, 0f);
            _arrowPivot = pivotGO.transform;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SafeDirectionArrowMat" };
            mat.SetColor("_BaseColor", ArrowColor);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", ArrowColor * 1.6f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            _arrowMat = mat;

            // A soft, unlit halo disc sitting under the arrow head. It has no
            // heading information of its own (it doesn't rotate with the pivot's
            // yaw) - purely a bright "look here" beacon so the guidance is easy to
            // notice at a glance and doesn't get lost against busy terrain colours,
            // independent of which way the arrow itself is currently pointing.
            var haloMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "SafeDirectionHaloMat" };
            if (haloMat.HasProperty("_BaseColor")) haloMat.SetColor("_BaseColor", ArrowColor * 0.9f);
            var haloGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            haloGO.name = "ArrowHalo";
            Object.Destroy(haloGO.GetComponent<Collider>());
            haloGO.transform.SetParent(pivotGO.transform, false);
            haloGO.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            haloGO.transform.localScale = new Vector3(0.55f, 0.12f, 0.55f);
            haloGO.GetComponent<Renderer>().sharedMaterial = haloMat;
            _arrowHalo = haloGO.transform;

            // ProceduralMeshFactory's cone has its apex along local +Y and base ring
            // at Y=0. Tilting it 90 deg about X remaps that apex direction onto local
            // +Z ("forward"), so afterwards yaw-rotating the *pivot* around world Y
            // (see Update) correctly aims the apex at the computed compass heading.
            var coneGO = new GameObject("ArrowHead");
            coneGO.transform.SetParent(pivotGO.transform, false);
            coneGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var mf = coneGO.AddComponent<MeshFilter>();
            // Slightly larger than before (0.32/1f -> 0.38/1.15f) for unambiguous
            // direction at a glance.
            mf.sharedMesh = ProceduralMeshFactory.CreateCone(0.38f, 1.15f, 8);
            var mr = coneGO.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            _arrowHead = coneGO.transform;

            var shaftGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaftGO.name = "ArrowShaft";
            Object.Destroy(shaftGO.GetComponent<Collider>());
            shaftGO.transform.SetParent(pivotGO.transform, false);
            shaftGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaftGO.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            shaftGO.transform.localScale = new Vector3(0.13f, 0.55f, 0.13f);
            shaftGO.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private void SetArrowVisible(bool visible)
        {
            if (_arrowPivot != null) _arrowPivot.gameObject.SetActive(visible);
        }
    }
}
