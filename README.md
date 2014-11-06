<!-- NOTE -->
<!-- Since git ui doesn't display the md correctly; Export this document to README.html using MarkdownPad 2 and modify html by removing 
    html, head and body tags manually
    body css 
-->


## Summary

SignalR.MagicHub is a publish-subscribe framework built on top of ASP.NET SignalR, which is an Open Source Microsoft.NET-based WebSockets framework. WebSockets is an Internet standard created in 2011 that provides for a realtime communication channels to be open between a web browser and a server. MagicHub makes it possible to use a single WebSocket channel to transmit messages and data of any topic to UI components on the browser. Without MagicHub each component would have to open its own channel, which is inefficient on the browser and requires more server capacity in data center.

## Pre-requiste 
Read [ASP.NET SignalR Documentation](http://www.asp.net/signalr/overview/getting-started)

## Developer Setup
    
	git clone git@git:SignalR.MagicHub
	cd SignalR.MagicHub
    build && build install


## Design

### Component Diagrams
[<img src="http://wiki/download/attachments/225249570/ComponentArchitecture-WebSocketManagement.png?api=v2" />](http://wiki/download/attachments/225249570/ComponentArchitecture-WebSocketManagement.png?api=v2)

### Network Topology
[<img src="http://wiki/download/attachments/225249570/NetworkTopology-WebSocketManagement.png?api=v2" />](http://wiki/download/attachments/225249570/NetworkTopology-WebSocketManagement.png?api=v2)

## Authentication

By default MagicHub requires user to be authenticated. `Context.User.Identity.IsAuthenticated` is used in authentication.

To disable authentication: sample Startup 
	
	public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
			GlobalHost.Configuration.AllowAnonymous();
		}
    }

For further documentation on how authentication works; see [SignalR Hub Authorizaton](http://www.asp.net/signalr/overview/security/hub-authorization)	

## Authorization

To add topic level authorization, Implement `IAuthorize` and register in startup 

	GlobalHost.DependencyResolver.Register(typeof(IAuthroize), () => ClaimsAuthrorization());


See [SignalR Extensibility](https://github.com/SignalR/SignalR/wiki/Extensibility) on how to replace dependency resolver. 

## Message Bus

To listen and send messages coming on the message bus to browser clients, implement `IMessageBus` and register

	GlobalHost.DependencyResolver.Register(typeof(IMessageBus), () => MessageBus());


## Client Usage
To integrate with magic-hub browser side including following javascript on the page

	<script src="scripts/jquery.magichub.min.js></script> 

#### Dependencies
* jquery
* json parser

For convenience magic-hub javascript packages SignalR and dynamically generated hub javascript into one file. 

### How to establish a connection

Magic hub doesn't establish connection unless explicitly started. To start magic hub call start:

    $.connection.magicHub.start(url)
        .done(function(){ console.log('Connected'); })
        .fail(function(){ console.log('Houston, We have a problem'); });

<dl>
	<dt>url</dt>
	<dd>string</dd>
	<dd>optional</dd>
	<dd>The <code>url</code> of the websocket domain. If url is not passed in magic-hub defaults to dynamic domain.</dd> 
</dl>

### How to subscribe to a topic

    $.connection.magicHub.on(topic, filter, callback)
        .done(function() { console.log('Thank you for subscribing'); })
        .fail(function() { console.log('Why no like?'); } );


<dl>
    <dt>topic</dt>
    <dd>string</dd>
    <dd>The <code>topic</code> defines the event that you would be interested to get messages on.
    </dd>
	<dt>filter</dt>
	<dd>string</dd>
	<dd>optional</dd>
	<dd>The <code>filter</code> defines the condition that you would be interested to get messages on. Filters are defined using SQL 92 syntax and typically apply to message headers. </dd>
    <dt>callback</dt>
    <dd>function</dd>
    <dd><code>callback</code> would be called when magic hub receives a message on the <code>topic</code></dd>
</dl>

#### Example

    $.connection.magicHub.on('echo', function(topic, data) { 
        alert("callback for echo"); });

-

    $.connection.magicHub.on('echo', "PatientID='1'",  function(topic, data) { 
        alert("callback for echo patientid 1"); });


### How to send a message

    $.connection.magicHub.send(topic, message)
        .done(function() { console.log('Thank you for sending message'); })
        .fail(function() { console.log('What's up with you?'); } );


<dl>
    <dt>topic</dt>
    <dd>string</dd>
    <dd>The <code>topic</code> defines the event <code>message </code>is applicable to.
    </dd>
    <dt>message</dt>
    <dd>json</dd>
    <dd><code>message</code> preferred format is json</dd>
</dl>


#### Example

	$.connection.magicHub.send('echo', { message: "hello world!" });


### How to unsubscribe

    $.connection.magicHub.off(topic, filter, callback)
        .done(function() { console.log('Thank you for unsubscribing'); })
        .fail(function() { console.log('Why no like?'); } );


<dl>
    <dt>topic</dt>
    <dd>string</dd>
    <dd>The <code>topic</code> defines the event that you would like to unsubscribe on.
	</dd>
	<dt>filter</dt>
	<dd>string</dd>
	<dd>optional</dd>
	<dd>The <code>filter</code> defines the condition that you would be interested to get messages on. Filters are defined using SQL 92 syntax and typically apply to message headers. </dd>
	<dt>callback</dt>
    <dd>function</dd>
    <dd><code>callback</code> reference should be the same one that was used in subscription</dd>
</dl>

#### Example
    var callback = function(topic, data) { alert("callback for echo"); };
    $.connection.magicHub.off('echo', callback);
	
-

    var callback = function(topic, data) { alert("callback for echo"); };
    $.connection.magicHub.off('echo', "PatientID='1'", callback);


Please remove this section if not applicable.  
Overview (or diagram) of integration approach of this application with other components.

## Filters
At the time of subscription for topic, filters can be provided as an argument. Filters follow the <a href="http://activemq.apache.org/selectors.html">SQL 92</a> syntax. 

SignalR.MagicHub doesn't do the filtering. Filtering is expected to be done at the MessageHub level for scalability. 

For example: 
If you use ActiveMQ as the message bus, filtering can be provided by the ActiveMQ using [JMS Selectors](http://activemq.apache.org/selectors.html). 
 

## Session Management
For long polling each requests go through the authentication; but for websockets once the channel is established even though session is expired the channel would not expire. To secure this there is an intelligent background worker that runs in-process which detects the session expiration. Once the session expiration is detected all subscriptions for activemq for that session is disconnected. All messages to that channel are stopped from server side. 

### Configure Session Provider

Implement `ISessionStateProvider` and register.

	GlobalHost.DependencyResolver.Register(typeof(ISessionStateProvider), () => SessionStateProvider());


## Source Address Affinity Routing
[<img src="http://wiki/download/attachments/225249570/SignalRMagicHub-Sticky-Sessions-Message-Flow.png?api=v2" />](http://wiki/download/attachments/225249570/SignalRMagicHub-Sticky-Sessions-Message-Flow.png?api=v2)


## Exception Handling / Logging / Tracing

### Exceptions

#### Client
Any exceptions on client side are logged to browser console window.

### Tracing
Messages can be traced by setting  `tracing_enabled:true` in the json body. When tracing is enabled in the message, the message is logged. On the server side message is logged to Trace listener; On the client side message is logged to console window. 

See [how to enable tracing](http://www.asp.net/signalr/overview/testing-and-debugging/enabling-signalr-tracing). 
