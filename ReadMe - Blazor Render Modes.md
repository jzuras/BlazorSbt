# Blazor Render Modes, Interactive Routing, and Enhanced Navigation

The render mode concept is new to this version of Blazor. Prior versions allowed creating an app that was entirely Blazor Server or entirely Blazor WebAssembly (WASM). This version allows an app to be both at the same time, and render modes can be set per page and per component. (There is also a workaround to set the mode app-wide (called globally) and then override it, as discussed later.)

There are several render modes: Static Server, Interactive Server, Interactive Auto, and Interactive WebAssembly (WASM). The Interactive part describes when and where the component can interact with the user, in terms of the Blazor runtime. (The resulting HTML can always be interactive using JavaScript.)

A related term is pre-rendering, which has some overlap with Static Server. Pre-rendering is when the initial HTML is created as fast as possible within the Blazor Server, and passed to the browser for display. At this point the page is considered static, meaning that there is no Blazor-bsaed interactivity. 

(Static Server can also use something called Streaming, which is useful when the static content may be large. This allows the HTML content to be displayed as it is generated. I have not yet used this feature.)

All Interactive rendering modes can use pre-rendering, but it is optional. If used, the initially displayed HTML is static and is not interactive until the Blazor runtime re-renders the component on the browser side (and creates a Signal R connection if needed for Interactive Server mode). If this takes a while to complete, the user could click on a button and wonder why nothing is happening.

Interactive Server means that the C# code (which makes the component interactive) runs on the Blazor Server using SignalR. Interactive WebAssembly means that the C# code runs entirely within the browser's Blazor runtime.

Back to pre-rendering, which is both a blessing and a curse. The blessing is the ability to display HTML to the user while waiting for the browser-side Blazor runtime to re-render in an interactive mode. The curse is a possible flash seen by the user as the re-rendered HTML replaces the pre-rendered HTML, if the HTML is different across the two renderings.

One reason there could be differences would be if the component displays data from a slow source such as a database. If it is not too slow, we could have the component load the full data during pre-rendering, at the cost of loading that same data when it is rendered a second time within the browser. But if the delay is too long for a user to stare at a blank space, the pre-rendering could be a progress indication such as "Loading..." which is later replaced by the data from a single database call.

An example of this is the Standings/Schedule page in this app. The page includes two components, one for standings and one for the schedule. The standings component loads all of its data during pre-render and again a second time (a future enhancement would be to cache this data). The schedule component uses a Radzen data grid which shows "Loading..." text until the full data is later loaded.

This causes a flash when the second rendering replaces the first, as the full schedule requires a scrollbar on the page. The added scrollbar makes the standings table shift sideways to make room (thus the flash). I handle this using a CSS styling option called scrollbar gutter, which supposedly does not work on iOS (I never tried it).

(Note - a reminder that the reason my components use different strategies is that this project is used to learn about different ways of doing things. A production app would likely make a choice and stick with it.)

Making things even more complicated/interesting is another new feature called Enhanced Navigation (EN), which is the ability to do partial refreshes instead of full-page loads. I initially expected that this would avoid running code that was not needed. However, I noticed that the NavMenu was reconstructed every time, even though it was already present on the page, which meant my assumption was incorrect.

In the default render mode of static, Blazor does a full page regeneration, but the Fetch method used by EN allows it to only update the part of the DOM which has changed. To get something more like what I wanted I had to also add Interactive Routing, which is when the Routes/Router component has a render mode set. (Note - this component's render mode defaults to Static if it is not set.) Setting the component's render mode is also described as setting a global render mode (this is what the Visual Studio template does when this flag is set when creating the project.)

With the combination of Enhanced Navigation and Interactive Routing, the NavMenu is not reconstructed when changing pages. Another example where this is useful is the Standings/Schedule page. When changing teams via the selection list, this navigates to the same page, so the page and components are not reconstructed - only their OnParametersSet method is called.

Although all the construction of components happens on the server in both cases (Static and Interactive render mode for the Routes/Router component), the Interactive mode maintains a SignalR connection to the server, and the components still exist within the server.  In Static mode, the server disposes the components it creates, after sending the HTML to the client. The Interactive mode will dispose of the components only when the user leaves the page.

Interactive Routing will skip rendering the components within the server (except for the first load or a forced reload). Because the Blazor runtime is already up and running within the client, it can skip the Pre-rendering and go straight to Interactive rendering.

Before discussing the workaround I mentioned earlier, here is the problem it addresses. I have a page that sets the render mode for one of its components to WASM. My first version was running without a render mode set, so it defaulted to Static render mode. The page had no issues when running in Static mode.

However, when the app runs in InteractiveServer render mode, a runtime error is thrown when the page attempts to create a WASM-mode component. Although the docs say to not try to apply a different interactive mode to a child component than its parent render mode, they go on to show us [how to do it anyway](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0#area-folder-of-static-ssr-components).

What they really mean is don't do this if the parent is running in an interactive mode - only in static mode will this work. The question is how to get the parent into static mode, and the answer is to force a reload of the page. Then the App razor component becomes involved, with a Property used as the render mode for the Routes component. This property can return null to force static mode when needed for the page that forced a re-load, at which point it can create a WASM component. See the StaningsList.razor page component in the Shared project of this repo for the code.
