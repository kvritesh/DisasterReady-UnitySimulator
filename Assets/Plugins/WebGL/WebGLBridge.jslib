// DisasterReady - minimal WebGL <-> host page bridge.
//
// Additive integration glue only: this file does not touch any gameplay,
// mission, scoring, or emergency-trigger logic. It exists purely so the
// Unity WebGL build can (a) read which region/scenario the web app asked
// it to launch with, via URL query parameters, and (b) hand the finished
// preparedness result back to the web page that opened it, via
// window.postMessage. If neither a query string nor an opener/parent
// window is present (e.g. the build is opened directly), both functions
// degrade harmlessly - GetLaunchParams returns defaults baked in on the
// C# side, and SendResultToBrowser simply has no window to post to.
mergeInto(LibraryManager.library, {

  DR_GetLaunchParams: function () {
    var params = new URLSearchParams(window.location.search);
    var obj = {
      regionId: params.get('regionId') || 'aizawl-mizoram',
      scenarioId: params.get('scenarioId') || 'preparedness-default',
      simulatorMode: params.get('simulatorMode') || 'standard'
    };
    var json = JSON.stringify(obj);
    var bufferSize = lengthBytesUTF8(json) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(json, buffer, bufferSize);
    return buffer;
  },

  DR_SendResultToBrowser: function (jsonPtr) {
    var json = UTF8ToString(jsonPtr);
    try {
      var data = JSON.parse(json);
      var message = Object.assign({ type: 'disasterready-simulator-result' }, data);
      var targetOrigin = window.location.origin;
      if (window.opener) {
        window.opener.postMessage(message, targetOrigin);
      } else if (window.parent && window.parent !== window) {
        window.parent.postMessage(message, targetOrigin);
      } else {
        console.log('[DisasterReady WebGLBridge] No opener/parent window to receive result; result was:', data);
      }
    } catch (e) {
      console.error('[DisasterReady WebGLBridge] Failed to send result to browser:', e);
    }
  }

});
