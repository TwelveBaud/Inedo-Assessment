using EntMgr;
using System.Runtime.CompilerServices;

// Our Framework code has to be strong-named in order to be granted full trust
// in the sandbox. It can only rely on strong-named libraries, so it can't
// rely on EntMgr.dll as provided. Thus, these definitions have moved to a
// strong-named library. However, existing customer code doesn't link against
// a strong-named library. This library exists to have the same name as
// customer code as looking for.

[assembly: TypeForwardedTo(typeof(IEntity))]
[assembly: TypeForwardedTo(typeof(IUpdatedableEntity))]
[assembly: TypeForwardedTo(typeof(IEntityMetadata))]
[assembly: TypeForwardedTo(typeof(IEntityRelationship))]
[assembly: TypeForwardedTo(typeof(IEntitySingletonRelationship))]
[assembly: TypeForwardedTo(typeof(IEntityMultiplexRelationship))]
[assembly: TypeForwardedTo(typeof(IEntityValidationRuleset))]
[assembly: TypeForwardedTo(typeof(IEntityValidationResults))]
[assembly: TypeForwardedTo(typeof(IEntityManager))]
