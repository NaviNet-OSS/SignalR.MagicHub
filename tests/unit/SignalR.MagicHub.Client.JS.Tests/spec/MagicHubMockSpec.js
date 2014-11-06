///<reference path="../Scripts/jasmine.js"/>
///<reference path="../../../../src/SignalR.MagicHub/scripts/jquery-1.9.1.js"/>
///<reference path="../Scripts/jasmine-utility.js"/>
///<reference path="../../../../src/SignalR.MagicHub/scripts/jquery.magicHub.mock.js"/>

describe("Magic hub mock", function() {
    it("should contain on method", function() {
        expect($.connection.magicHub.on).toBeDefined();
    });

    it("should contain off method", function() {
        expect($.connection.magicHub.off).toBeDefined();
    });

    it("should contain send method", function() {
        expect($.connection.magicHub.send).toBeDefined();
    });

    it("should contain start method", function() {
        expect($.connection.magicHub.start).toBeDefined();
    });

    describe("start", function() {
        it("should return the resolved promise", function () {
            //Act
            var promise = $.connection.magicHub.start();
            
            //Assert
            expect(promise.state()).toEqual("resolved");
        });
    });
    
    describe("on", function () {
        it("should return the resolved promise", function () {
            //Act
            var promise = $.connection.magicHub.on("foo", function() {
                return true;
            });

            //Assert
            expect(promise.state()).toEqual("resolved");
        });
        
        it("filter should return the resolved promise", function () {
            //Act
            var promise = $.connection.magicHub.on("foo", "patientid='bar'", function () {
                return true;
            });

            //Assert
            expect(promise.state()).toEqual("resolved");
        });
    });
    
    describe("send", function () {
        it("should callback", function () {
            //Arrange
            var callbackCalled = false;
            $.connection.magicHub.on("foo", function () {
                callbackCalled = true;
            });

            //Act
            $.connection.magicHub.send("foo", { "message": "bar" });
            
            //Assert
            expect(callbackCalled).toBeTruthy();
        });
    });
    

    describe("off", function () {
        it("should stop callbacks", function () {
            //Arrange
            var count = 0;
            var callback = function() { count = count +1; };

            //Act
            $.connection.magicHub.on("foo", callback);
            $.connection.magicHub.send("foo", { "message": "bar" });
            $.connection.magicHub.off("foo", callback);
            $.connection.magicHub.send("foo", { "message": "bar" });

            //Assert
            expect(count).toEqual(1);
        });

    });
    

});