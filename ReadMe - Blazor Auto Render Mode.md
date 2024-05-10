# Blazor Auto Render Mode

Below is my experience with InteractiveAuto render mode, which allows an automatic transition from server mode to client mode. But first a note about how the [official documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing?view=aspnetcore-8.0) can be confusing.

The MS Learn documentation "ASP.NET Core Blazor render modes" says two different things about Interactive Auto. The table near the top says it means "Interactive Server using Blazor Server initially and then WASM on subsequent visits after the Blazor bundle is downloaded." However, this does not seem to be accurate. Or perhaps it is the truth but not the whole truth.

The same documentation says further down that "Auto mode prefers to select a render mode that matches the render mode of existing interactive components. The reason that the Auto mode prefers to use an existing interactivity mode is to avoid introducing a new interactive runtime that doesn't share state with the existing runtime."

I have now experienced both of these descriptions. 

Originally, my StandingsList page tried to create a WASM component even though it was running in InteractiveServer mode, which threw a runtime error. When I switched to InteractiveAuto mode it worked, but not how I expected.

When I added print statements to check the modes for the page and components, I learned that the component had been created in InteractiveServer mode, just like the page itself. So this is a match to the second description in the documentation.

My observations when running my WASM project with a global render mode of InteractiveAuto were quite illuminating. 

When I have made a change to my code, the pages will all display InteractiveServer (SSR) mode, but only until I access the Scores page (which forces WASM mode, which the user will see as CSR), or do a forced reload of a page. The forced reload will cause the page to first use Pre-render mode, and then switch to CSR. Once the app has loaded a CSR page, it remains in that mode from that point on. This matches the initial description in the documentation.

Interestingly, if I have not made a change to my code before running it via the debugger, it immediately goes into CSR mode. I assume this is because it still has a cached version of the code that it can use.

Based on my observations, I left my WASM project in Auto mode, as this seems to use CSR mode soon enough, in my opinion.
