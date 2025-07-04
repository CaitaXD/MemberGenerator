# MemberGenerator

Generates partial interface members inside the type that implements the interface.

## Usage

```csharp
[MemberGenerator.GenerateDefaultMembers]
public interface IDefaultInterface
{
    public void Foo() => Console.WriteLine("Foo");
    public int Bar() => 42;
}
```

So when implementing the interface

```csharp
public partial struct MyStruct : IDefaultInterface;
```

Will generate the following partial type

```csharp
public partial struct MyStruct
{
    public void Foo() => Console.WriteLine("Foo");
    public int Bar() => 42;
}
```

## Important

The type that implements the interface must be <b>partial</b>, otherwise nothing will be generated.

## Use case

Default interface members cannot be called directly from the type that implements the interface,
as it requires the instance to be cast to the interface type.

Additionally, if you do this in struct, it can cause it to be boxed.