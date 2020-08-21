using EntMgr;
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(IEntity))]
[assembly: TypeForwardedTo(typeof(IUpdatedableEntity))]
[assembly: TypeForwardedTo(typeof(IEntityMetadata))]
[assembly: TypeForwardedTo(typeof(IEntityRelationship))]
[assembly: TypeForwardedTo(typeof(IEntitySingletonRelationship))]
[assembly: TypeForwardedTo(typeof(IEntityMultiplexRelationship))]
[assembly: TypeForwardedTo(typeof(IEntityValidationRuleset))]
[assembly: TypeForwardedTo(typeof(IEntityValidationResults))]
[assembly: TypeForwardedTo(typeof(IEntityManager))]
