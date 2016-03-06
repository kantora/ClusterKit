﻿namespace ClusterKit.Core.EF
{
    using System;
    using System.Data.Entity;
    using System.Linq;

    using Akka.Configuration;

    using Castle.MicroKernel.SubSystems.Configuration;
    using Castle.Windsor;

    /// <summary>
    /// Installing components from current library
    /// </summary>
    public class Installer : BaseInstaller
    {
        /// <summary>
        /// Gets priority for ordering akka configurations. Highest priority will override lower priority.
        /// </summary>
        /// <remarks>Consider using <seealso cref="BaseInstaller"/> integrated constants</remarks>
        protected override decimal AkkaConfigLoadPriority => BaseInstaller.PrioritySharedLib;

        public override void PreCheck(Config config)
        {
            BaseEntityFrameworkInstaller installer = null;
            try
            {
                var installerType =
                    AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .Single(t => t.IsSubclassOf(typeof(BaseEntityFrameworkInstaller)));

                installer = (BaseEntityFrameworkInstaller)installerType.GetConstructor(new Type[0])?.Invoke(new object[0]);
                if (installer == null)
                {
                    throw new InvalidOperationException();
                }
            }
            catch (InvalidOperationException)
            {
                throw new ConfigurationException("There should be exactly one EntityFrameworkInstaller in plugins with public parameterless construcot");
            }

            DbConfiguration.SetConfiguration(installer.GetConfiguration());
        }

        /// <summary>
        /// Gets default akka configuration for current module
        /// </summary>
        /// <returns>Akka configuration</returns>
        protected override Config GetAkkaConfig() => ConfigurationFactory.ParseString("{}");

        /// <summary>
        /// Registering DI components
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="store">The configuration store.</param>
        protected override void RegisterWindsorComponents(IWindsorContainer container, IConfigurationStore store)
        {
        }
    }
}