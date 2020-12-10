# GremlinStudio
My take on making an Enterprise Query Studio like SQL Server Management Studio, but this is primarily for queries.

But why WPF?  Because no one wants to enter their cosmos db credentials in a web page, they want to use a fat client, and WPF is much better then WinForms.  UWP is an option, but I wanted to simplify the project.

Functionality for Gremlin Studio includes:
- Gremlin query shortcuts
- Gremlin snippets
- Json result tab (when executing)
- Graph result tab (when executing)
- Vertex properties window (when clicking on Vertex)
- Vertex edge details (when clicking on Vertex)
- View Gremlin query execution plan

## [Download Executable](https://gremlineditor.azurewebsites.net) or Build from Code

## Basic Gremlin Query
![](res/screenshotMain.png)

## Viewing Results as Vertices and Edges
![](res/screenshotGraph.png)
