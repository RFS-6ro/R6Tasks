# R6Tasks
This repo provides few tasks extensions and solution for parallelizing tasks with dependencies. 

Parallelizing tasks solution is based on book "Concurrency in .NET" https://www.manning.com/books/concurrency-in-dot-net

Built-in Example:
Here is an oriented graph, presenting tasks and their dependencies
![image](https://user-images.githubusercontent.com/56915388/179741683-8028fec8-15b6-4068-9fce-087fdb6df2d4.png)

Using R6Parallelizer we initiate system:

```csharp
R6Parallelizer resolver = new R6Parallelizer();

resolver.AddTask(1, action(1, 6000), new[] {4, 5});
resolver.AddTask(2, action(2, 2000), new[] {5});
resolver.AddTask(3, action(3, 8000), new[] {6, 5});
resolver.AddTask(4, action(4, 5000), new[] {6});
resolver.AddTask(5, action(5, 4500), new[] {7, 8});
resolver.AddTask(6, action(6, 1000), new[] {7});
resolver.AddTask(7, action(7, 9000));
resolver.AddTask(8, action(8, 7000));
```

We could subscribe on task complete events

```csharp
resolver.OnTaskCompleted += ResolverOnOnTaskCompleted;
```

And Resolve it with a single line

```csharp
resolver.Resolve();
```


# Output

![image](https://user-images.githubusercontent.com/56915388/179742241-5f33c213-ac70-4c04-b6c9-5e19b9da77ed.png)

# Note
R6Parallelizer automatically reuse free resources

# Dependencies
This solution uses my fork of Actress repository.
link: https://github.com/RFS-6ro/Actress
