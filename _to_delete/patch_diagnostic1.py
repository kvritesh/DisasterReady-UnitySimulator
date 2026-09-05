import sys, io

path = r"Assets/Scripts/Editor/KayKitLayoutDiagnostic.cs"
with io.open(path, "r", encoding="utf-8") as f:
    src = f.read()

replacements = []

# Trigger colliders (MissionZoneTrigger's SphereCollider on Hospital/Shelter,
# radius 7) are SUPPOSED to reach the route - that's how a mission fires as
# the player walks past. They don't physically block the CharacterController
# (Unity never resolves collision against a trigger), so flagging them as
# "blocking" was a false positive. Also skip the Fence's "Rail" collider:
# it's a single long thin box that can be rotated at any angle, so its
# axis-aligned Bounds can report a huge apparent "radius" even though the
# rail itself is thin (an AABB of a diagonal box captures far more than the
# box's true width) - the individual "Post_N" colliders along the same run
# already represent the fence's real blocking footprint accurately, so the
# Rail entry is redundant noise on top of being mismeasured.
replacements.append((
'''            void CollectColliders(Transform root, string kind)
            {
                if (root == null) return;
                foreach (var col in root.GetComponentsInChildren<Collider>())
                {
                    obstacles.Add((col.transform.name + " (under " + root.name + ")", kind, col.bounds));
                }
            }''',
'''            void CollectColliders(Transform root, string kind)
            {
                if (root == null) return;
                foreach (var col in root.GetComponentsInChildren<Collider>())
                {
                    // Triggers (e.g. the 7m MissionZoneTrigger SphereCollider
                    // on Hospital/Shelter) are meant to reach the route - they
                    // don't physically block movement, so they're not a
                    // "blocking obstacle" in the sense this check cares about.
                    if (col.isTrigger) continue;
                    // The fence Rail's axis-aligned Bounds wildly overstates
                    // its true (thin) footprint once rotated off-axis; the
                    // individual Post_N colliders along the same run already
                    // cover this check accurately.
                    if (col.transform.name == "Rail") continue;
                    obstacles.Add((col.transform.name + " (under " + root.name + ")", kind, col.bounds));
                }
            }'''
))

errors = []
for i, (old, new) in enumerate(replacements):
    count = src.count(old)
    if count != 1:
        errors.append(f"replacement #{i} matched {count} times (expected 1)")
        continue
    src = src.replace(old, new, 1)

if errors:
    sys.stderr.write("PATCH FAILED:\n" + "\n".join(errors) + "\n")
    sys.exit(1)

with io.open(path, "w", encoding="utf-8") as f:
    f.write(src)

print("Patched KayKitLayoutDiagnostic.cs successfully.")
