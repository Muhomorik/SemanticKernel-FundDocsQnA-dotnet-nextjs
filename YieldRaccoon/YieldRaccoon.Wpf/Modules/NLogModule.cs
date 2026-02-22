using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Pipeline;
using NLog;

namespace YieldRaccoon.Wpf.Modules;

/// <summary>
/// Autofac module that automatically injects type-aware <see cref="NLog.ILogger"/> instances
/// into all registered components. Eliminates the need for per-registration
/// <c>WithParameter(new ResolvedParameter(...))</c> boilerplate.
/// </summary>
public class NLogModule : Module
{
    protected override void AttachToComponentRegistration(
        IComponentRegistryBuilder componentRegistry,
        IComponentRegistration registration)
    {
        registration.PipelineBuilding += (sender, pipeline) =>
        {
            pipeline.Use(PipelinePhase.ParameterSelection, MiddlewareInsertionMode.StartOfPhase, (context, next) =>
            {
                var limitType = context.Registration.Activator.LimitType;
                context.ChangeParameters(context.Parameters.Union(
                [
                    new ResolvedParameter(
                        (pi, _) => pi.ParameterType == typeof(ILogger),
                        (pi, _) => LogManager.GetLogger(limitType.FullName!)),
                ]));

                next(context);
            });
        };
    }
}
