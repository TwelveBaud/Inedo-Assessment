using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

// This has the entity manager interfaces in a place that our strong-named code can
// link against, as described in ProxyNotations.cs. Since this library is strong-named,
// our code can take a dependency on it, and since that library is weak-named but has
// forwarding declarations to this one, Guru code dependencies are undisrupted.

// Why is this in a separate library from our code? Our code has full trust, and .NET
// doesn't allow partial trust code to override full trust code. Although I might be
// able to get away with applying APTCA to our code and marking all this as transparent,
// that has security implications; farming it out to a strongly-named but untrusted
// assembly like this one is easier and more secure.

namespace EntMgr
{
    [TypeForwardedFrom("EntMgr, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public interface IEntity
    {
        Guid ID { get; }
    }

    [TypeForwardedFrom("EntMgr, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public interface IUpdatedableEntity : IEntity
    {
    }

    [TypeForwardedFrom("EntMgr, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public interface IEntityMetadata : IEntity
    {
        string LastModifedBy { get; }
        DateTimeOffset LastUpdated { get; }
    }

    [TypeForwardedFrom("EntMgr, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public interface IEntityRelationship : IEntity
    {
    }

    [TypeForwardedFrom("EntMgr, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public interface IEntitySingletonRelationship : IEntityRelationship, IEntity
    {
        new Guid ID { get; }
    }

    [TypeForwardedFrom("EntMgr, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public interface IEntityMultiplexRelationship : IEntityRelationship, IEntity
    {
        Guid[] IDs { get; }
    }

    [TypeForwardedFrom("EntMgr, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public interface IEntityValidationRuleset : IEntity
    {
    }

    [TypeForwardedFrom("EntMgr, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public interface IEntityValidationResults
    {
        bool IsValid { get; }
        string Message { get; }
    }

    [TypeForwardedFrom("EntMgr, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public interface IEntityManager
    {
        void Load(IEntity entity);
        IUpdatedableEntity GetWriteable();
        IEntityMetadata GetMetadata();
        Guid GetRelationship(string type);
        IEnumerable<IEntityValidationResults> Validate(IEntityValidationRuleset rules);
        void WriteEDFStream(TextWriter writer);
        void Unload();
    }
}
