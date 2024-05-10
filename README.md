# Blazor SoftballTech

This is the Overview of the project. I also have ReadMe files for Blazor Render Modes (in general) and Auto Render Mode specifically.

This is a partially complete app and still a work-in-progress, so there are still some to-do comments in the code, as well as future-reference commented-out code.

There are a couple of LinkedIn writeups from the first release of this project, [here](https://www.linkedin.com/feed/update/urn:li:share:7187454509382533121/) and [here](https://www.linkedin.com/feed/update/urn:li:share:7187506342943731713/). That version used static rendering and only server-side database access. 

This Blazor version of my SoftballTech project was written to learn more about the way things work within Blazor. To that end, it does things that would not typically be done in a production environment, just to see if it can be done.

As of May 2024, the site consists of only a few pages and components:
1) The Home page, of course. The important part of this is the description of which global render mode is being used. The only component used here is the Dynamic Header, used on all pages.
2) The Standings List page. There are actually two links to this page in the Nav Menu, one for Softball and another for Hockey. The page treats Hockey differently: the StandingsList component is created using a Render Mode of WASM for Hockey Leagues. For Softball, no mode is given to the component, so it uses the global mode. (No interactivity is needed here, so a Static mode is acceptable.) The Standings List Component displays the data using HTML elements.
3) The Standings and Schedule Page. The page uses two components: a Standings Component, which uses Blazor's QuickGrid, and a Schedule Component, which uses Radzen's DataGrid and Blazor's InputSelect. The Standings are not interactive, but the Schedule can be filtered on a single team. The page creates the Schedule component in InteractiveServer mode when the page is itself being rendered in static mode; otherwise, the page's current rendering mode is used.
4) The Scores Page. This page uses a Scores Header component, which then uses a Scores Single component. The page forces the component to run in WASM rendering mode, which requires the page to force a reload if it is being run in InteractiveServer mode. InteractiveServer mode cannot create a WASM component, but the forced reload will run the page in Static mode so it can then create a WASM component. (This workaround is described in the official MS Learn documentation, and it requires code in the App component as well as the page itself. Note - the official workaround describes a better reason for its use.)

I also wanted to learn how the above worked in various global render modes. The available modes are Static (Server-Side), InteractiveServer, InteractiveWASM, and InteractiveAuto. Trying them all required several projects.

This solution consists of 6 projects: a server/client pair using InteractiveAuto, another pair that uses InteractiveServer in Debug and Static in Release, a shared library project that contains all of the pages and components, plus the model and services for accessing the DB, and lastly an API project to access the database from code running in the browser.

The Debug vs Release difference is created via a constant defined within the project settings in Debug (but not in Release), which in turn sets a Feature Flag. However, the pages that set their component's render modes need to know which project they are a part of so they can act accordingly. They are told this by means of a Service which is injected by both the Client and the Server projects. This could probably be made cleaner or more flexible, but for now it is a single boolean specifying WASM or not.

When the app is run, the User will notice out-of-place text in red that describes the current render mode. The render mode for the page is at the top, while each component has text of its own, since the render mode for components may be different. The text will make it obvious when Static mode (described by the text as Pre-render) is replaced by an Interactive mode (SSR or CSR for server or browser/client).

The version deployed on [Azure](https://blazorsbt.azurewebsites.net/) is the Static rendering version (displayed as Pre-render on the pages). This version uses other modes as well: the Interactive Server mode (SSR) is used for the Schedule component, and the WASM mode (CSR) for the Scores component, and the StandingsList component for Hockey. Note that the rendering mode for the page remains Pre-render even for these cases.

The LinkedIn writeup for this version is [here](https://www.linkedin.com/feed/update/urn:li:share:7194695208704397313/).

Before wrapping up, I want to give a huge shout-out to Shaun Curtis, as his Blazr.RenderState library was a big help. His repo is here on GH and it is available as a nuget package to include in projects.


