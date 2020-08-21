using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

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
