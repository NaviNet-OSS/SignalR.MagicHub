/*

TODO: ADD Documentation

This file allows to share single channel by all client and server app communication.

To subscribe to a topic    
	$.connection.magicHub.on(topic, filter, callback).done().fail()
		topic       any string value
        filter      sql92 string
		callback    callback function for a message is received on the topic

	Example:
		$.connection.magicHub.on('echo', function(topic, data) {alert("callback for echo"); });

To unsubscribe
	$.connection.magicHub.off(topic, callback).done().fail()
		topic       any string value
		callback    callback function for a message is received on the topic
		
	Example:
		$.connection.magicHub.off('echo', function(topic, data) {alert("callback for echo"); });

To send message
	$.connection.magicHub.send(topic, data).done().fail()
		topic       any string value
		data        message for the topic in json format
		
	Example:
		$.connection.magicHub.send('echo', jsonMessage);

		To send message
	$.connection.magicHub.start(options).done().fail()
		topic       any string value
		data        message for the topic in json format
		
	Example:
		$.connection.magicHub.send('echo', jsonMessage);

*/
/// <reference path="Scripts/jquery-1.9.1.js" />
(function ($, window) {
    "use strict";

    if (typeof ($) !== "function") {
        // no jQuery!
        throw new Error("SignalR.MagicHub: jQuery not found. Please ensure jQuery is referenced before the SignalR.js file.");
    }

    if (!window.JSON) {
        // no JSON!
        throw new Error("SignalR.MagicHub: No JSON parser found. Please ensure json2.js is referenced before the SignalR.js file if you need to support clients without native JSON parsing support, e.g. IE<8.");
    }

    if (!$.connection) {
        throw new Error("SignalR.MagicHub: SignalR not found. Please ensure jquery.signalR is reference before the jquery.magicHub.js file");
    }

    if (!$.connection.topicBroker) {
        throw new Error("SignalR.MagicHub: SignalR hub not found. Please ensure jquery.hub is reference before the jquery.magicHub.js file");
    }

    var _subscriptionStore = function () {
        var topicsToFilters = {};
        var NO_FILTER = "<nofilter>";

        var _getTopicFilters = function (topic) {
            return topicsToFilters[topic] = topicsToFilters[topic] || {};
        };
        var _getTopicFilterCallbacks = function (topic, filter) {
            var filterDict = topicsToFilters[topic] = topicsToFilters[topic] || {};
            var callbacks = filterDict[filter] = filterDict[filter] || [];

            return callbacks;
        };

        var _on = function (topic, filter, callback) {
            filter = filter || NO_FILTER;
            _getTopicFilterCallbacks(topic, filter).push(callback);
        };

        var _off = function (topic, filter, callback) {
            if (!callback && filter && typeof filter === 'function') {
                callback = filter;
                filter = undefined;
            }

            if (filter) {
                var callbacks = _getTopicFilterCallbacks(topic, filter);
                var idx = callbacks.indexOf(callback);

                if (idx > -1) {
                    delete callbacks[idx];
                    // todo: should we clean up callbacks from its container when it's empty?
                }
            } else {
                var filterDict = _getTopicFilters(topic);

                for (var filter in filterDict) {
                    var callbacks = filterDict[filter] || [];
                    for (var idx = callbacks.length - 1; idx >= 0; idx--) {
                        var existingCallback = callbacks[idx];
                        if (existingCallback == callback) {
                            delete callbacks[idx];
                        }
                    }
                }
            }
        };

        var _trigger = function (topic, filter, message) {
            if (!message && filter) {
                message = filter;
                filter = null;
            }
            var callbacks = _getTopicFilterCallbacks(topic, filter || NO_FILTER);

            for (var i = 0; i < callbacks.length; i++) {
                var callback = callbacks[i];

                if (callback) {
                    var e = $.Event(topic);
                    callback.apply(this, [e, message]);
                }
            }
        };

        return {
            on: _on,
            off: _off,
            trigger: _trigger
        };
    };

    $.magicHub = { debug: false };
    $.signalR.ajaxTimeout = 5000; //setting to 5s to get the server warm up.

    var _topicBroker = $.connection.topicBroker,
        _currentState = $.signalR.connectionState.connecting;

    var _topics = [],
        _deferredTopics = [],
        _filterCallbacks = new _subscriptionStore();

    var _isHubConnectionDoneCalled = false,
        _isHubConnectionFailedCalled = false,
        _startDeferred = null;

    var logger = {
        log: function (msg, level, logging) {
            if (logging === false) {
                return;
            }
            if (typeof (window.console) === "undefined") {
                return;
            }
            var formatParameters = {
                datetime: new Date().toISOString(),
                source: 'SignalR.MagicHub',
                level: level || 'debug',
                message: msg
            };

            var format = '{datetime} {source}: [{level}] {message}';
            for (var key in formatParameters) {
                format = format.replace('{' + key + '}', formatParameters[key]);
            }
            if (window.console.debug) {
                window.console.debug(format);
            } else if (window.console.log) {
                window.console.log(format);
            }
        }
    };

    var _parseUrl = function (url) {
        var a = document.createElement('a');
        a.href = url;
        return {
            href: a.href,
            host: a.host || location.host,
            port: ('0' === a.port || '' === a.port) ? location.port : a.port,
            hash: a.hash,
            hostname: a.hostname || location.hostname,
            pathname: a.pathname.charAt(0) != '/' ? '/' + a.pathname : a.pathname,
            protocol: !a.protocol || ':' == a.protocol ? location.protocol : a.protocol,
            search: a.search,
            query: a.search.slice(1)
        };
    };

    var _isCrossDomain = function (url) {
        url = _parseUrl(url);
        return url.hostname !== location.hostname
            || url.port !== location.port
            || url.protocol !== location.protocol;
    };

    //Send the subscription to the server. If the subscription fails rejects the call else calls the done.
    var _sendSubscription = function (topic, filter, deferred) {
        try {
            _topicBroker.server.subscribe(topic, filter).done(_getDoneCallback(deferred)).fail(_getFailCallback(deferred));

            _topics.push({ 'topic': topic, 'filter': filter });
        } catch (e) {
            deferred.reject(e);
        }
    };

    //Resend the subscription to the server. If the subscription fails rejects the call else calls the done.
    var _resendSubscription = function (topic, filter) {
        _topicBroker.server.subscribe(topic, filter);
    };

    //retry the deferred topics
    var _retryDeferredSubscriptions = function () {
        if (_deferredTopics.length > 0) {
            if (_isHubConnectionFailedCalled) {
                //reject all subscriptions
                $.each(_deferredTopics, function () {
                    this.deferred.reject();
                });
                _deferredTopics = [];
            } else if (_isHubConnectionDoneCalled && _currentState === $.signalR.connectionState.connected) {
                //send subscriptions
                $.each(_deferredTopics, function () {
                    _sendSubscription(this.topic, this.filter, this.deferred);
                });
                _deferredTopics = [];
            } else {
                //try again
                setTimeout(_retryDeferredSubscriptions, 1000);
            }
        }
    };

    var _dispatch = function (event, data) {
        //Triggers the message on the topic
        _filterCallbacks.trigger(event, data);
    };

    _topicBroker.client.serverOrderedDisconnect = function (retry) {
        $.connection.hub.stop();
        //if retry set try after timeout
        if (retry) {
            //try resending

        }
    };

    //handle server onmessage calls
    _topicBroker.client.onmessage = function (topic, filter, message) {
        // log for trace
        try {
            var jsonData = JSON.parse(message);
            if (jsonData.tracing_enabled === true) {
                logger.log("Received topic: " + topic + "; message: " + message + (filter ? ("; filter: " + filter) : ""), 'trace');
            }
            //Triggers the message on the topic
            _filterCallbacks.trigger(topic, filter, jsonData);
        } catch (ex) {
            logger.log("Error parsing received message;" + ex);
        }
    };

    var _getFailCallback = function (deferred) {
        return function (e) {
            if ($.magicHub.debug) {
                logger.log(e, 'trace');
            }
            deferred.reject(e);
        };
    };

    var _getDoneCallback = function (deferred) {
        return function () {
            deferred.resolve();
        };
    };


    //api starts hereon

    //send the message to server
    var _send = function (topic, data, headers) {
        //Sends the message to the server. 
        //Returns a chainable promise object.    
        var deferred = $.Deferred();

        if ($.connection.hub.state !== $.signalR.connectionState.connected) {
            deferred.reject("SignalR.MagicHub: Connection must be fully connected before a message can be sent.");
            return deferred.promise();
        }

        // log for trace
        var stringData = JSON.stringify(data);
        try {
            if (data.tracing_enabled === true) {
                logger.log("Sending topic: " + topic + "; message: " + stringData, 'trace');
            }
        } catch (ex) { }

        try {
            //serialize data 
            _topicBroker.server.send(topic, stringData, headers).done(_getDoneCallback(deferred)).fail(_getFailCallback(deferred));
        } catch (e) {
            logger.log(e.message);
            deferred.reject(e);
        }
        return deferred.promise();
    };

    //If the connection is open sends the subscription right away. Else caches the topic and sends the subscription on connect. 
    var _on = function (topic, filter, callback) {
        // Check and shift arguments
        if (!callback && filter && typeof filter == 'function') {
            callback = filter;
            filter = undefined;
        }
        // 

        //Returns a chainable promise object.    
        var deferred = $.Deferred();
        _filterCallbacks.on(topic, filter, callback);

        if (topic !== '/state/change') {
            if (_currentState === $.signalR.connectionState.connected) {
                //send the subscription right away
                _sendSubscription(topic, filter, deferred);
            } else {
                if (_isHubConnectionFailedCalled) {
                    //reject the subscription right away as the hub connection failed
                    deferred.reject();
                } else {
                    //cache for deferred sending
                    _deferredTopics.push({ "topic": topic, "filter": filter, "deferred": deferred });
                }
            }
        } else {
            deferred.resolve();
        }
        return deferred.promise();
    };

    //unsubscribe topics
    var _off = function (topic, filter, callback) {
        // Check and shift arguments
        if (!callback && filter && typeof filter === 'function') {
            callback = filter;
            filter = undefined;
        }
        // 

        //Turns off the subscriber from subscription on topic.
        var deferred = $.Deferred();
        _filterCallbacks.off(topic, filter, callback);

        if (topic !== '/state/change') {
            if (_currentState === $.signalR.connectionState.connected) {
                _topicBroker.server.unsubscribe(topic, filter).done(_getDoneCallback(deferred)).fail(_getFailCallback(deferred));
            } else {
                if (_isHubConnectionFailedCalled) {
                    deferred.reject();
                } else {
                    // todo: remove from deferred?
                }
            }
        }

        return deferred.promise();
    };

    //start the connection using the appropriate transport and url.
    var _start = function (options) {

        if (_startDeferred) {
            //if deferred object is initialized return
            //second time call
            return _startDeferred.promise();
        }
        _startDeferred = $.Deferred();

        var config = {
            waitForPageLoad: false,
            transport: "auto",
            jsonp: false
        };

        $.extend(config, options);

        if (config.url) {
            $.connection.hub.url = _parseUrl(options.url).href;

            //if cross domain set jsonp
            if (_isCrossDomain(options.url)) {
                //set jsonp to true to support cross domain ajax call
                config.jsonp = !$.support.cors
                if (config.transport === "auto") {
                    // Try webSockets and longPolling since SSE doesn't support CORS and ForeverFrame needs document.domain
                    config.transport = ['webSockets', 'longPolling'];
                }
            }
        }

        //set the url
        $.connection.hub.error(function (e) {            
            if ((e.source.status && e.source.responseText) && e.source.status === 500 && e.source.responseText.indexOf("The user identity cannot change during ") > -1) {
                logger.log("Session expiration error detected. Emitting session close message.");
                $.connection.topicBroker.client.onmessage("nnt:session/expired", null, JSON.stringify({code: "EXPIRED"}));
            }
        });
        $.connection.hub.start(config).done(function () {
            //On connection start send the cached subscriptions
            _isHubConnectionDoneCalled = true;
            //hack for http; need warm up after connection
            _send("debug/mode", { "message": "55AE6F46CFB643F68CD3128A6561C18555AE6F46CFB643F68CD3128A6561C18555AE6F46CFB643F68CD3128A6561C185" });
            if (_topics.length > 0) {
                logger.log("Sending subscription from queue for topic: " + JSON.stringify(_topics));
            }
            $.each(_deferredTopics, function () {
                _sendSubscription(this.topic, this.filter, this.deferred);
            });
            _deferredTopics = [];
            _startDeferred.resolve();
        }).fail(function (e) {
            //If fails in connecting to server reject all the subscriptions
            _isHubConnectionFailedCalled = true;
            logger.log("Failed in starting connection");
            $.each(_deferredTopics, function () {
                this.deferred.reject(e);
            });
            //clear all deferred topics
            _deferredTopics = [];
            _startDeferred.reject(e);
        });
        return _startDeferred.promise();
    };

    //disconnect the connection and reset state
    var _reset = function () {

        //close the connection
        $.connection.hub.stop();
        _startDeferred = null;
        _filterCallbacks = new _subscriptionStore();
        _topics = [];
        _deferredTopics = [];
        _currentState = $.signalR.connectionState.connecting;

        _isHubConnectionDoneCalled = false,
        _isHubConnectionFailedCalled = false;
    };


    //Listen for the state change
    $.connection.hub.stateChanged(function (state) {
        _currentState = state.newState;
        _dispatch('/state/change', state);
    });

    //resend the subscriptions
    $.connection.hub.reconnected(function () {
        // SignalR 2.0 has a problem with the reconnected method in websockets. 
        // Calls do hub don't fire unless you have a delay.
        // this should be fixed in 2.1.0
        // ref: https://github.com/SignalR/SignalR/issues/2642

        var resubscribe = function () {
            //send the topics again
            logger.log("Resending subscription from queue for topic(s): " + _topics);
            $.each(_topics, function (key, topicHash) {
                _resendSubscription(topicHash.topic, topicHash.filter);
            });

            //retry the topics
            _retryDeferredSubscriptions();

            //hack for http; need warm up after reconnection
            _send("debug/mode", { "message": "55AE6F46CFB643F68CD3128A6561C18555AE6F46CFB643F68CD3128A6561C18555AE6F46CFB643F68CD3128A6561C185" });
        };

        if ($.connection.hub.transport.name != "webSockets") {
            resubscribe();
        } else {
            setTimeout(resubscribe, 5);
        }
    });

    var _extensionMethods = {
        magicHub: {
            on: _on,
            off: _off,
            send: _send,
            start: _start,
            //internal usage
            reset: _reset
        }
    };

    $.extend(true, $.connection, _extensionMethods);
}(window.jQuery, window));
