///<reference path="../Scripts/jasmine.js"/>
///<reference path="../../../../src/SignalR.MagicHub/scripts/jquery-1.9.1.js"/>
///<reference path="../../../../src/SignalR.MagicHub/scripts/jquery.signalR-2.2.0.js"/>
///<reference path="../scripts/hubs.js"/>
///<reference path="../Scripts/jasmine-utility.js"/>
///<reference path="../../../../src/SignalR.MagicHub/scripts/jquery.magicHub.core.js"/>

describe("Magic hub", function () {
    it("should contain on method", function () {
        expect($.connection.magicHub.on).toBeDefined();
    });

    it("should contain off method", function () {
        expect($.connection.magicHub.off).toBeDefined();
    });

    it("should contain send method", function () {
        expect($.connection.magicHub.send).toBeDefined();
    });

    it("should contain start method", function () {
        expect($.connection.magicHub.start).toBeDefined();
    });

    it("should contain reset method", function () {
        expect($.connection.magicHub.reset).toBeDefined();
    });

    describe("start", function () {
        var connectionHub = $.connection.hub;


        beforeEach(function () {
            $.connection.magicHub.reset();
            var deferredStart = $.Deferred();
            spyOn(connectionHub, "start").andReturn(deferredStart.promise());
        });

        afterEach(function () {
            //reset all spies
            connectionHub.start.reset();
        });
        
        it("should reuse the promise", function() {
            var promise1 = $.connection.magicHub.start();
            var promise2 = $.connection.magicHub.start();

            expect(promise2).toEqual(promise1);
        });
    });

    describe("subscription", function () {

        var connectionHub = $.connection.hub;


        beforeEach(function () {
            $.connection.magicHub.reset();
        });

        afterEach(function () {
            //reset all spies
            connectionHub.start.reset();
        });


        it("should send subscription when connection is open", function () {

            var serverTopicBroker = $.connection.topicBroker.server;
            var callback = jasmine.createSpy('callback'),
                failCallback = jasmine.createSpy('failCallback'),
                doneCallback = jasmine.createSpy('doneCallback');

            var deferredStart = $.Deferred(),
                deferredSubscribe = $.Deferred();
            //complete start
            deferredStart.resolve();

            spyOn(connectionHub, "start").andReturn(deferredStart.promise());
            spyOn(serverTopicBroker, "subscribe").andReturn(deferredSubscribe.promise());

            runs(function () {
                $.connection.magicHub.reset();
                $.connection.magicHub.start().done(function () {
                    expect(connectionHub.start).toHaveBeenCalled();
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    //should send subscription right away
                    $.connection.magicHub.on("topic", callback).done(doneCallback).fail(failCallback);
                });
            });

            runs(function () {
                deferredSubscribe.resolve();
                //expect done to be called now
                expect(doneCallback).toHaveBeenCalled();
            });

            runs(function () {
                serverTopicBroker.subscribe.reset();
            });

        });

        it("should delay subscription until connection is open", function () {

            var serverTopicBroker = $.connection.topicBroker.server;
            var callback = jasmine.createSpy('callback'),
                failCallback = jasmine.createSpy('failCallback'),
                doneCallback = jasmine.createSpy('doneCallback');

            var deferredStart = $.Deferred(),
                deferredSubscribe = $.Deferred();

            spyOn(connectionHub, "start").andReturn(deferredStart.promise());
            spyOn(serverTopicBroker, "subscribe").andReturn(deferredSubscribe.promise());

            runs(function () {
                $.connection.magicHub.start().done(function () {
                    expect(connectionHub.start).toHaveBeenCalled();
                });
            });

            runs(function () {
                deferredSubscribe.resolve();
                //start delayed subscription
                $.connection.magicHub.on("topic", callback).done(doneCallback).fail(failCallback);
            });

            runs(function () {
                expect(doneCallback).not.toHaveBeenCalled();
                //complete start
                deferredStart.resolve();
            });

            runs(function () {
                //expect done to be called now
                expect(doneCallback).toHaveBeenCalled();
            });

            runs(function () {
                serverTopicBroker.subscribe.reset();
            });

        });

        it("should delay subscription until connection is reopen", function () {
            var serverTopicBroker = $.connection.topicBroker.server;
            var callback = jasmine.createSpy('callback'),
                failCallback = jasmine.createSpy('failCallback'),
                doneCallback = jasmine.createSpy('doneCallback');

            var callback2 = jasmine.createSpy('callback2'),
                failCallback2 = jasmine.createSpy('failCallback2'),
                doneCallback2 = jasmine.createSpy('doneCallback2');

            var deferred = $.Deferred(),
                deferredSubscribe = $.Deferred();
            
            //complete start
            deferred.resolve();

            spyOn(connectionHub, "start").andReturn(deferred.promise());
            spyOn(serverTopicBroker, "subscribe").andReturn(deferredSubscribe.promise());
            $.connection.hub.transport = { name: "longPolling", abort: function() {}, stop: function () {} };
            runs(function () {
                $.connection.magicHub.reset();
                $.connection.magicHub.start().done(function () {
                    expect(connectionHub.start).toHaveBeenCalled();
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    //should send subscription right away
                    $.connection.magicHub.on("topic", callback).done(doneCallback).fail(failCallback);
                });
            });

            runs(function () {
                deferredSubscribe.resolve();
                //expect done to be called now
                expect(doneCallback).toHaveBeenCalled();
            });

            runs(function () {
                //change state to reconnecting
                $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.reconnecting }]);

                //subscribe to topic2
                $.connection.magicHub.on("topic2", callback2).done(doneCallback2).fail(failCallback2);
            });

            runs(function () {
                //expect done to be not called
                expect(doneCallback2).not.toHaveBeenCalled();
            });


            runs(function () {
                //now change the state to connected and trigger reconnect event
                $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.reconnecting, newState: $.signalR.connectionState.connected }]);
                //trigger the reconnect event
                $($.connection.hub).trigger("onReconnect");
            });

            runs(function () {
                expect(doneCallback2).toHaveBeenCalled();
            });
        });

        it("should call fail callback when subscription to a topic fails", function () {
            var deferred = $.Deferred();

            //reject the connection
            spyOn(connectionHub, "start").andReturn(deferred.promise());
            deferred.reject();

            var callback = jasmine.createSpy(),
                failCallback = jasmine.createSpy(),
                doneCallback = jasmine.createSpy();

            runs(function () {
                $.connection.magicHub.start();
            });

            runs(function () {
                $.connection.magicHub.on("topic", callback).done(doneCallback).fail(failCallback);
            });

            runs(function () {
                expect(failCallback).toHaveBeenCalled();
            });
        });

        it("should send subscription with filter when connection is open", function () {

            var serverTopicBroker = $.connection.topicBroker.server;
            var callback = jasmine.createSpy('callback'),
                failCallback = jasmine.createSpy('failCallback'),
                doneCallback = jasmine.createSpy('doneCallback');

            var deferredStart = $.Deferred(),
                deferredSubscribe = $.Deferred();
            //complete start
            deferredStart.resolve();

            spyOn(connectionHub, "start").andReturn(deferredStart.promise());
            spyOn(serverTopicBroker, "subscribe").andReturn(deferredSubscribe.promise());

            runs(function () {
                $.connection.magicHub.reset();
                $.connection.magicHub.start().done(function () {
                    expect(connectionHub.start).toHaveBeenCalled();
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    //should send subscription right away
                    $.connection.magicHub.on("topic", "myfilter", callback).done(doneCallback).fail(failCallback);
                });
            });

            runs(function () {
                deferredSubscribe.resolve();
                //expect done to be called now
                expect(doneCallback).toHaveBeenCalled();
            });

            runs(function () {
                serverTopicBroker.subscribe.reset();
            });

        });

        it("should resend subscription after reconnect", function() {
            var serverTopicBroker = $.connection.topicBroker.server;
            var callback = jasmine.createSpy('callback'),
                failCallback = jasmine.createSpy('failCallback'),
                doneCallback = jasmine.createSpy('doneCallback');

            var deferredStart = $.Deferred(),
                deferredSubscribe = $.Deferred();
            //complete start
            deferredStart.resolve();
            deferredSubscribe.resolve();
            
            spyOn(connectionHub, "start").andReturn(deferredStart.promise());
            spyOn(serverTopicBroker, "subscribe").andReturn(deferredSubscribe.promise());
            
            runs(function () {
                $.connection.magicHub.reset();
                $.connection.magicHub.start().done(function () {
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    //should send subscription right away
                    $.connection.magicHub.on("topic", "myfilter", callback).done(doneCallback).fail(failCallback);
                });
            });
            
            runs(function () {
                //change state to reconnecting
                //now change the state to connected and trigger reconnect event
                $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.reconnecting, newState: $.signalR.connectionState.connected }]);
                //trigger the reconnect event
                $($.connection.hub).trigger("onReconnect");
            });
            
            runs(function () {
                //reconnect
                expect(serverTopicBroker.subscribe).toHaveBeenCalledWith('topic', 'myfilter');
                expect(serverTopicBroker.subscribe.callCount).toBe(2);
            });

            runs(function () {
                serverTopicBroker.subscribe.reset();
            });


        });
    });

    describe("send", function () {

        it("should call fail callback when sending a message fails", function () {

            var failCallback = jasmine.createSpy(),
                doneCallback = jasmine.createSpy();

            runs(function () {
                $.connection.magicHub.send("topic", { "message": "foo" }).done(doneCallback).fail(failCallback);
            });

            runs(function () {
                expect(failCallback).toHaveBeenCalled();
            });
        });

        it("should send a message when connected", function () {
            var failCallback = jasmine.createSpy('failCallback'),
                doneCallback = jasmine.createSpy('doneCallback');

            var deferredSend = $.Deferred();

            var serverTopicBroker = $.connection.topicBroker.server;
            spyOn(serverTopicBroker, "send").andReturn(deferredSend.promise());

            runs(function () {
                deferredSend.resolve();
                $.connection.changeState($.connection.hub, $.signalR.connectionState.disconnected, $.signalR.connectionState.connected);
                $.connection.magicHub.send("topic", { "message": "foo" }).done(doneCallback).fail(failCallback);
            });

            runs(function () {
                expect(serverTopicBroker.send).toHaveBeenCalled();
                expect(doneCallback).toHaveBeenCalled();
            });

            runs(function () {
                //reset
                serverTopicBroker.send.reset();
            });
        });

        it("should log messages with trace", function () {
            var failCallback = jasmine.createSpy(),
                doneCallback = jasmine.createSpy();

            var serverTopicBroker = $.connection.topicBroker.server;
            $.connection.changeState($.connection.hub, $.signalR.connectionState.disconnected, $.signalR.connectionState.connected);
            spyOn(serverTopicBroker, "send");
            spyOn(console, "log");
            var debugSpy = null;
            if (console.debug)
                debugSpy = spyOn(console, "debug");

            var testDate = new Date(Date.parse("Thu May 30 2013 14:49:57 GMT-0400 (Eastern Daylight Time)"));

            var dateSpy = spyOn(window, 'Date').andReturn(testDate);

            runs(function () {
                $.connection.magicHub.send("topic", { "message": "foo", "tracing_enabled": true, "messageId": "123" }).done(doneCallback).fail(failCallback);
            });

            runs(function () {

                var msg = '2013-05-30T18:49:57.000Z SignalR.MagicHub: [trace] Sending topic: topic; message: {"message":"foo","tracing_enabled":true,"messageId":"123"}';
                if (console.debug)
                    expect(console.debug).toHaveBeenCalledWith(msg);
                else
                    expect(console.log).toHaveBeenCalledWith(msg);
                //expect(doneCallback).toHaveBeenCalled();
            });

            runs(function () {
                //reset
                serverTopicBroker.send.reset();
                console.log.reset();
                debugSpy && debugSpy.reset();
                dateSpy.reset();

            });
        });

    });

    describe("receive", function () {

        var deferred = $.Deferred(),
            deferredUnsubscribe = $.Deferred();
        var connectionHub = $.connection.hub;
        var serverTopicBroker = $.connection.topicBroker.server;

        beforeEach(function () {
            spyOn(connectionHub, "start").andReturn(deferred.promise());
            spyOn(serverTopicBroker, "subscribe");
            spyOn(serverTopicBroker, "unsubscribe").andReturn(deferredUnsubscribe.promise());
            deferred.resolve();
            //reset is used to isolate unit of work. reset is not needed in live code as we will be sharing the connection.
            $.connection.magicHub.reset();
        });

        afterEach(function () {
            //reset all spies
            connectionHub.start.reset();
            serverTopicBroker.subscribe.reset();
            serverTopicBroker.unsubscribe.reset();
        });

        it("should call callback when a message is available for the subscribed topic", function () {

            var callback = jasmine.createSpy();
            var failCallback = jasmine.createSpy(),
                doneCallback = jasmine.createSpy();

            runs(function () {
                $.connection.magicHub.start().done(function () {
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    $.connection.magicHub.on("topic", callback).done(doneCallback).fail(failCallback);
                });
            });

            runs(function () {
                //var data = { "M": [{ "H": "TopicBroker", "M": "onmessage", "A": ["topic", "42"] }] };

                //trigger the message 
                $.connection.topicBroker.client.onmessage("topic", null, "37");
            });

            runs(function () {
                expect(callback).toHaveBeenCalled();
            });
        });
        
        it("should receive a callback with proper jQuery event object", function () {

            var callback = jasmine.createSpy().andCallFake(function(e, message) {
                expect(e.type).toEqual("topic");
                expect(e.timeStamp).not.toBeUndefined();
            });
            var failCallback = jasmine.createSpy(),
                doneCallback = jasmine.createSpy();

            runs(function () {
                $.connection.magicHub.start().done(function () {
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    $.connection.magicHub.on("topic", callback).done(doneCallback).fail(failCallback);
                });
            });

            runs(function () {

                //trigger the message 
                $.connection.topicBroker.client.onmessage("topic", null, "37");
            });

            runs(function () {
                expect(callback).toHaveBeenCalled();
            });
        });


        it("should not call callback when topic unsubscribed", function () {

            var isCallbackState = 0;
            var callback = function () { isCallbackState = 1; };
            var failCallback = jasmine.createSpy('failCallback'),
                doneCallback = jasmine.createSpy('doneCallback');

            runs(function () {
                $.connection.magicHub.start().done(function () {
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    $.connection.magicHub.on("topic", callback).done(doneCallback).fail(failCallback);
                });
            });

            runs(function () {
                //var data = { "M": [{ "H": "TopicBroker", "M": "onmessage", "A": ["topic", "42"] }] };

                //trigger the message 
                $.connection.topicBroker.client.onmessage("topic", null, "37");
            });

            runs(function () {
                //make sure that callback has been called
                expect(isCallbackState).toBe(1);
                //reset 
                isCallbackState = 2;
            });

            runs(function () {
                //now unsubscribe to topic
                $.connection.magicHub.off("topic", callback);
                //trigger the message 
                $.connection.topicBroker.client.onmessage("topic", null, "37");
            });

            runs(function () {
                //check when unsubscribe message is not called 
                expect(isCallbackState).toBe(2);
            });
        });
        
        it("should log messages with trace", function () {

            var failCallback = jasmine.createSpy(),
                doneCallback = jasmine.createSpy();

            spyOn(console, "log");
            var debugSpy = null;
            if (console.debug)
                debugSpy = spyOn(console, "debug");


            var testDate = new Date(Date.parse("Thu May 30 2013 14:49:57 GMT-0400 (Eastern Daylight Time)"));

            var dateSpy = spyOn(window, 'Date').andReturn(testDate);

            runs(function () {
                //reset is used to isolate unit of work. reset is not needed in live code as we will be sharing the connection.
                $.connection.magicHub.reset();
                $.connection.magicHub.start().done(function () {
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    $.connection.magicHub.on("topic").done(doneCallback).fail(failCallback);
                });
            });

            runs(function () {
                var msg = JSON.stringify({ "message": "foo", "tracing_enabled": true, "messageId": "123" });

                //trigger the message 
                $.connection.topicBroker.client.onmessage("topic", null, msg);
            });

            runs(function () {
                var msg = '2013-05-30T18:49:57.000Z SignalR.MagicHub: [trace] Received topic: topic; message: {"message":"foo","tracing_enabled":true,"messageId":"123"}';
                var logFunc = console.debug || console.log;
                expect(logFunc).toHaveBeenCalled();
                expect(logFunc.mostRecentCall.args[0]).toEqual(msg);
            });

            runs(function () {
                //reset
                console.log.reset();
                debugSpy && debugSpy.reset();
                dateSpy.reset();

            });

        });
        
        it("should call callback when a message is available for the subscribed topic with a filter", function () {

            var callback = jasmine.createSpy();
            var failCallback = jasmine.createSpy(),
                doneCallback = jasmine.createSpy();

            runs(function () {
                $.connection.magicHub.start().done(function () {
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    $.connection.magicHub.on("topic", "filter1", callback).done(doneCallback).fail(failCallback);
                });
            });

            runs(function () {
                //trigger the message 
                $.connection.topicBroker.client.onmessage("topic", "filter1", "{\"message\": 37 }");
            });

            runs(function () {
                expect(callback).toHaveBeenCalled();
            });
        });

        it("should not call callback when topic with filter unsubscribed", function () {

            var isCallbackState = 0;
            var callback = function () { isCallbackState = 1; };
            var failCallback = jasmine.createSpy('failCallback'),
                doneCallback = jasmine.createSpy('doneCallback');

            runs(function () {
                $.connection.magicHub.start().done(function () {
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    $.connection.magicHub.on("topic", "filter1", callback).done(doneCallback).fail(failCallback);
                });
            });

            runs(function () {
                //trigger the message 
                $.connection.topicBroker.client.onmessage("topic", "filter1", "{\"message\": 37}");
            });

            runs(function () {
                //make sure that callback has been called
                expect(isCallbackState).toBe(1);
                //reset 
                isCallbackState = 2;
            });

            runs(function () {
                //now unsubscribe to topic
                $.connection.magicHub.off("topic", callback);
                //trigger the message 
                $.connection.topicBroker.client.onmessage("topic", null, "37");
            });

            runs(function () {
                //check when unsubscribe message is not called 
                expect(isCallbackState).toBe(2);
            });
        });
        
        it("should unsubscribe from all filters when .off is called without a filter", function () {

            var callback = jasmine.createSpy();
            var callback2 = jasmine.createSpy();
            var failCallback = jasmine.createSpy('failCallback'),
                doneCallback = jasmine.createSpy('doneCallback');

            runs(function () {
                $.connection.magicHub.start().done(function () {
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    $.connection.magicHub.on("topic", "filter1", callback).done(doneCallback).fail(failCallback);
                    $.connection.magicHub.on("topic", "filter2", callback).done(doneCallback).fail(failCallback);
                    $.connection.magicHub.on("topic", "filter1", callback2).done(doneCallback).fail(failCallback);
                    $.connection.magicHub.on("topic", "filter2", callback2).done(doneCallback).fail(failCallback);

                    // unsubscribe from all callback 1 for topic
                    $.connection.magicHub.off("topic", callback);
                    //trigger the message 
                });
            });

            runs(function () {
                
                $.connection.topicBroker.client.onmessage("topic", "filter1", "{\"message\": 37}");
            });

            runs(function () {
                //make sure that callback has been called
                expect(callback.callCount).toBe(0);
                expect(callback2.callCount).toBe(1);
            });

        });
        
        it("should call callback when a message is available for the subscribed topic with a filter after unsubscribing to a different filter for the same topic with the same callback", function () {

            var callback = jasmine.createSpy();
            var failCallback = jasmine.createSpy(),
                doneCallback = jasmine.createSpy();

            runs(function () {
                $.connection.magicHub.start().done(function () {
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    $.connection.magicHub.on("topic", "filter1", callback).done(doneCallback).fail(failCallback);
                    $.connection.magicHub.on("topic", "filter2", callback).done(doneCallback).fail(failCallback);
                });
            });

            runs(function () {
                //trigger the message 
                $.connection.topicBroker.client.onmessage("topic", "filter1", "{\"message\": 37}");
            });

            runs(function () {
                expect(callback.callCount).toBe(1);
            });
            
            runs(function () {
                // unsubscribe from the topic with the second filter
                $.connection.magicHub.off("topic", "filter2", callback);
                $.connection.topicBroker.client.onmessage("topic", "filter1", "{\"message\": 37}");
            });
            
            runs(function () {
                expect(callback.callCount).toBe(2);
            });
        });

        it("should call multiple callbacks when a message is available for the subscribed topic", function () {

            var callback = jasmine.createSpy();
            var callback2 = jasmine.createSpy();
            var failCallback = jasmine.createSpy(),
                doneCallback = jasmine.createSpy();

            runs(function () {
                $.connection.magicHub.start().done(function () {
                    $($.connection.hub).trigger("onStateChanged", [{ oldState: $.signalR.connectionState.connected, newState: $.signalR.connectionState.connected }]);
                    $.connection.magicHub.on("topic", "filter1", callback).done(doneCallback).fail(failCallback);
                    $.connection.magicHub.on("topic", "filter1", callback2).done(doneCallback).fail(failCallback);

                });
            });

            runs(function () {
                //trigger the message 
                $.connection.topicBroker.client.onmessage("topic", "filter1", "{\"message\": 37}");
            });

            runs(function () {
                expect(callback.callCount).toBe(1);
                expect(callback2.callCount).toBe(1);
            });
        });

    });

    describe("session", function() {
        it("should send expired when server gets a 500 error for changed itentity", function () {
            var onMessageSpy;
            runs(function() {
                // arrange
                $.connection.magicHub.start({ url: "//example.com" });
                onMessageSpy = spyOn($.connection.topicBroker.client, "onmessage");
                // act
                $($.connection.hub).trigger("onError", { source: { status: 500, responseText: "The user identity cannot change during " } });

                // assert
                expect(onMessageSpy).toHaveBeenCalledWith("nnt:session/expired", null, "{\"code\":\"EXPIRED\"}");
            });

            runs(function () {
                onMessageSpy.reset();
                $.connection.magicHub.reset();
            });
        });
    });

    describe("cross domain connection start", function () {

        var connectionHub = $.connection.hub;
        var corsSupported = $.support.cors; 
        beforeEach(function () {
            $.connection.magicHub.reset();
            spyOn(connectionHub, "start").andReturn($.Deferred().promise());
        });

        afterEach(function () {
            //reset all spies
            connectionHub.start.reset();
            $.support.cors = corsSupported;
        });

        it("should use jsonp for // url", function() {
            //Arrange
            $.support.cors = false;
            var config = {
                    waitForPageLoad: false,
                    transport: ['webSockets', 'longPolling'],
                    jsonp: true,
                    url: "//example.com"
                };

            //Act
            $.connection.magicHub.start({ url: "//example.com" });

            //Assert
            expect(connectionHub.start).toHaveBeenCalledWith(config);
        });
        
        it("should use jsonp for http:// url", function () {
            //Arrange
            $.support.cors = false;
            var config = {
                    waitForPageLoad: false,
                    transport: ['webSockets', 'longPolling'],
                    jsonp: true,
                    url: "http://example.com"
                };

            //Act
            $.connection.magicHub.start({ url: "http://example.com" });

            //Assert
            expect(connectionHub.start).toHaveBeenCalledWith(config);
        });
        
        it("should use jsonp for https:// url", function () {
            //Arrange
            $.support.cors = false;
            var config = {
                    waitForPageLoad: false,
                    transport: ['webSockets', 'longPolling'],
                    jsonp: true,
                    url: "https://example.com"
                };

            //Act
            $.connection.magicHub.start({ url: "https://example.com" });

            //Assert
            expect(connectionHub.start).toHaveBeenCalledWith(config);
        });

    });
});


