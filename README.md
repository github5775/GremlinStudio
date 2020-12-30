# GremlinStudio
My take on making an Enterprise Query Studio like SQL Server Management Studio, but this is primarily for queries.

But why WPF?  Users prefer to enter their cosmos db credentials in a fat client rather than someone' webpage, and WPF has a more robust UI then WinForms.  UWP is an option, but I wanted to simplify the project.

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
