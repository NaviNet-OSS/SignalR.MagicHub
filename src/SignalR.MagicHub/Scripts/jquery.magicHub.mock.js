(function($, window) {
    "use strict";
    
    if (typeof ($) !== "function") {
        // no jQuery!
        throw new Error("SignalR.MagicHub: jQuery not found. Please ensure jQuery is referenced.");
    }

    if (!window.JSON) {
        // no JSON!
        throw new Error("SignalR.MagicHub: No JSON parser found. Please ensure json2.js is referenced if you need to support clients without native JSON parsing support, e.g. IE<8.");
    }

    $.connection = $.connection || {};
    var callbacks = $({});
    
    var _start = function() {
        var deferred = $.Deferred();
        deferred.resolve();
        return deferred.promise();
    };

    var _on = function (topic, filter, callback) {
        // Check and shift arguments
        if (!callback && filter && typeof filter == 'function') {
            callback = filter;
            filter = undefined;
        }
        var deferred = $.Deferred();

        callbacks.on.apply(callbacks, [topic, callback]);
        deferred.resolve();

        return deferred.promise();
    };

    var _off = function(topic, filter, callback) {
        // Check and shift arguments
        if (!callback && filter && typeof filter === 'function') {
            callback = filter;
            filter = undefined;
        }
        
        callbacks.off.apply(callbacks, [topic, callback]);
    };

    var _send = function(topic, data) {
        var deferred = $.Deferred();

        callbacks.trigger.apply(callbacks, [topic, data]);
        deferred.resolve();

        return deferred.promise();
    };

    
    var _extensionMethods = {
        magicHub: {
            on: _on,
            off: _off,
            send: _send,
            start: _start,
        }
    };

    $.extend(true, $.connection, _extensionMethods);

}(window.jQuery, window));