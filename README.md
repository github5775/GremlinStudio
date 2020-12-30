# GremlinStudio
My take on making an Enterprise Query Studio like SQL Server Management Studio, but this is primarily for queries.

But why WPF?  Users prefer to enter their Cosmos DB credentials into a fat client (rather than someone's webpage), and WPF has a more robust UI then WinForms.  UWP is an option, but I wanted to simplify the project.

Functionality for Gremlin Studio includes:
- Gremlin query shortcuts
- Gremlin snippets
- Json results tab (when executing)
- Graph results tab (when executing)
- Vertex properties window (when clicking on Vertex)
- Edge properties window (when clicking on Vertex)
- View Gremlin query execution plans

## [Download Executable](https://gremlineditor.azurewebsites.net) or Build from Code

## Basic Gremlin Query
![](res/screenshotMain.png)

## Viewing Results as Vertices and Edges
![](res/screenshotGraph.png)

### Please bear in mind that I didn't address Graph DB design in this studio, other than providing the ability to easily view the actual execution plans.  In future versions I may add this functionality.
