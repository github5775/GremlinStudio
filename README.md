# GremlinStudio
## The underlying "GremlinClient" has changed, and I am updating the code to use NET 6 and GremlinClient 3.7.0.  Next step, update the studio code to use the latest version of the GremlinClient lib. ##

My take on making an Enterprise Query Studio like SQL Server Management Studio, but this is primarily for queries.

But why WPF?  Users prefer to enter their Cosmos DB credentials into a fat client (rather than someone's webpage), and WPF has a more robust UI then WinForms.  UWP is an option, but I wanted to simplify the project.  (NB: This tool works for partitions of type string; I am in the process of expanding it to partitions of type "number.")

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

#### * Please bear in mind that in this studio I didn't address Graph DB design, i.e. partitions, duplication, and ChangeFeed triggers, other than providing the ability to easily view the actual execution plans.  In future versions I may add this functionality.
