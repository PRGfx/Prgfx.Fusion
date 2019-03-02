# Prgfx.Fusion
This package tries to implement the DSL [Fusion](https://github.com/neos/typoscript) from the PHP CMS [Neos](https://neos.io) in C#.

Fusion enables the creation of powerfull templating constructions in the templating language itself, benefitting a component-based templating approach.

Documentation on language-features will be found mostly in the [official neos documentation](https://neos.readthedocs.io/en/stable/CreatingASite/Fusion/index.html) for now.

Currently missing are a port of the DSL [Eel](https://github.com/neos/eel/) and the caching implementation, as well as some utility functions regarding sorting of keys.

## Basic language concept
Starting from a rendering path, fusion will try to evaluate the construct assigned to that rendering path.
For each rendering path you can assign a simple value (e.g. `path = 'example'`, `path = true`), an eel-expression (e.g. `path = ${expression}`) or the instruction to render a _prototype_: `path = My.Namespace:SomePrototype`.

### Using Prototypes
A prototype always returns a value and may take arguments to generate that value. Arguments can be given to the prototype invokation in it's body:
```
path = My.Namespace:SomePrototype {
    arg1 = true
    arg2 = 4
    arg3 = ${variable}
    arg4 = My.Namespace:SomeOtherPrototype
}
```

### Declaring Prototypes
Prototypes are defined by either defining the `@class` property pointing at a class extending `Prgfx.Fusion.FusionObjects.AbstractFusionObject` or inheriting from a prototype eventually having a `@class` assigned:
```
prototype(My.Namespace:SomePrototype) {
    @class = 'Any.AccessibleType.Extending.AbstractFusionObject'
}
prototype(My.Namespace:SomeOtherPrototype) < prototype(MyNamespace:BasePrototype) {
    // additional properties
}
```

## On the namespace
The included default prototypes are called `Neos.Fusion:...` to allow for mostly seamless usage of existing components. Of course prototypes relying on the original framework will not be supported.

## Omitted language features
* As you will probably want to build your fusion source code yorself, including embedded resources, the `include: ` directive is not implemented.
* This implementation will not support the `namespace` directive.
