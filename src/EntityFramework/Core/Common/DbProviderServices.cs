namespace System.Data.Entity.Core.Common
{
    using System.Data.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.EntityClient;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Spatial;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// The factory for building command definitions; use the type of this object
    /// as the argument to the IServiceProvider.GetService method on the provider
    /// factory;
    /// </summary>
    [CLSCompliant(false)]
    public abstract class DbProviderServices
    {
        /// <summary>
        /// Create a Command Definition object given a command tree.
        /// </summary>
        /// <param name="commandTree">command tree for the statement</param>
        /// <returns>an exectable command definition object</returns>
        /// <remarks>
        /// This method simply delegates to the provider's implementation of CreateDbCommandDefinition.
        /// </remarks>
        public DbCommandDefinition CreateCommandDefinition(DbCommandTree commandTree)
        {
            Contract.Requires(commandTree != null);
            ValidateDataSpace(commandTree);
            var storeMetadata = (StoreItemCollection)commandTree.MetadataWorkspace.GetItemCollection(DataSpace.SSpace);
            Debug.Assert(storeMetadata.StoreProviderManifest != null, "StoreItemCollection has null StoreProviderManifest?");

            return CreateDbCommandDefinition(storeMetadata.StoreProviderManifest, commandTree);
        }

        /// <summary>
        /// Create a Command Definition object given a command tree.
        /// </summary>
        /// <param name="commandTree">command tree for the statement</param>
        /// <returns>an exectable command definition object</returns>
        /// <remarks>
        /// This method simply delegates to the provider's implementation of CreateDbCommandDefinition.
        /// </remarks>
        public DbCommandDefinition CreateCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
        {
            try
            {
                return CreateDbCommandDefinition(providerManifest, commandTree);
            }
            catch (ProviderIncompatibleException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (EntityUtil.IsCatchableExceptionType(e))
                {
                    throw new ProviderIncompatibleException(Strings.ProviderDidNotCreateACommandDefinition, e);
                }
                throw;
            }
        }

        /// <summary>
        /// Create a Command Definition object, given the provider manifest and command tree
        /// </summary>
        /// <param name="connection">provider manifest previously retrieved from the store provider</param>
        /// <param name="commandTree">command tree for the statement</param>
        /// <returns>an exectable command definition object</returns>
        protected abstract DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree);

        /// <summary>
        /// Ensures that the data space of the specified command tree is the target (S-) space
        /// </summary>
        /// <param name="commandTree">The command tree for which the data space should be validated</param>
        internal virtual void ValidateDataSpace(DbCommandTree commandTree)
        {
            Debug.Assert(commandTree != null, "Ensure command tree is non-null before calling ValidateDataSpace");

            if (commandTree.DataSpace
                != DataSpace.SSpace)
            {
                throw new ProviderIncompatibleException(Strings.ProviderRequiresStoreCommandTree);
            }
        }

        /// <summary>
        /// Create a DbCommand object given a command tree.
        /// </summary>
        /// <param name="commandTree">command tree for the statement</param>
        /// <returns>a command object</returns>
        internal virtual DbCommand CreateCommand(DbCommandTree commandTree)
        {
            var commandDefinition = CreateCommandDefinition(commandTree);
            var command = commandDefinition.CreateCommand();
            return command;
        }

        /// <summary>
        /// Create the default DbCommandDefinition object based on the prototype command
        /// This method is intended for provider writers to build a default command definition
        /// from a command. 
        /// Note: This will clone the prototype
        /// </summary>
        /// <param name="prototype">the prototype command</param>
        /// <returns>an executable command definition object</returns>
        public virtual DbCommandDefinition CreateCommandDefinition(DbCommand prototype)
        {
            return DbCommandDefinition.CreateCommandDefinition(prototype);
        }

        /// <summary>
        /// Retrieve the provider manifest token based on the specified connection.
        /// </summary>
        /// <param name="connection">The connection for which the provider manifest token should be retrieved.</param>
        /// <returns>
        /// The provider manifest token that describes the specified connection, as determined by the provider.
        /// </returns>
        /// <remarks>
        /// This method simply delegates to the provider's implementation of GetDbProviderManifestToken.
        /// </remarks>
        public string GetProviderManifestToken(DbConnection connection)
        {
            try
            {
                var providerManifestToken = GetDbProviderManifestToken(connection);
                if (providerManifestToken == null)
                {
                    throw new ProviderIncompatibleException(Strings.ProviderDidNotReturnAProviderManifestToken);
                }
                return providerManifestToken;
            }
            catch (ProviderIncompatibleException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (EntityUtil.IsCatchableExceptionType(e))
                {
                    throw new ProviderIncompatibleException(Strings.ProviderDidNotReturnAProviderManifestToken, e);
                }
                throw;
            }
        }

        protected abstract string GetDbProviderManifestToken(DbConnection connection);

        public DbProviderManifest GetProviderManifest(string manifestToken)
        {
            try
            {
                var providerManifest = GetDbProviderManifest(manifestToken);
                if (providerManifest == null)
                {
                    throw new ProviderIncompatibleException(Strings.ProviderDidNotReturnAProviderManifest);
                }

                return providerManifest;
            }
            catch (ProviderIncompatibleException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (EntityUtil.IsCatchableExceptionType(e))
                {
                    throw new ProviderIncompatibleException(Strings.ProviderDidNotReturnAProviderManifest, e);
                }
                throw;
            }
        }

        protected abstract DbProviderManifest GetDbProviderManifest(string manifestToken);

        public DbSpatialDataReader GetSpatialDataReader(DbDataReader fromReader, string manifestToken)
        {
            try
            {
                var spatialReader = GetDbSpatialDataReader(fromReader, manifestToken);
                if (spatialReader == null)
                {
                    throw new ProviderIncompatibleException(Strings.ProviderDidNotReturnSpatialServices);
                }

                return spatialReader;
            }
            catch (ProviderIncompatibleException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (EntityUtil.IsCatchableExceptionType(e))
                {
                    throw new ProviderIncompatibleException(Strings.ProviderDidNotReturnSpatialServices, e);
                }
                throw;
            }
        }

        public DbSpatialServices GetSpatialServices(string manifestToken)
        {
            try
            {
                var spatialServices = DbGetSpatialServices(manifestToken);
                if (spatialServices == null)
                {
                    throw new ProviderIncompatibleException(Strings.ProviderDidNotReturnSpatialServices);
                }

                return spatialServices;
            }
            catch (ProviderIncompatibleException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (EntityUtil.IsCatchableExceptionType(e))
                {
                    throw new ProviderIncompatibleException(Strings.ProviderDidNotReturnSpatialServices, e);
                }
                throw;
            }
        }

        protected virtual DbSpatialDataReader GetDbSpatialDataReader(DbDataReader fromReader, string manifestToken)
        {
            // Must be a virtual method; abstract would break previous implementors of DbProviderServices
            throw new ProviderIncompatibleException(Strings.ProviderDidNotReturnSpatialServices);
        }

        protected virtual DbSpatialServices DbGetSpatialServices(string manifestToken)
        {
            // Must be a virtual method; abstract would break previous implementors of DbProviderServices
            throw new ProviderIncompatibleException(Strings.ProviderDidNotReturnSpatialServices);
        }

        internal void SetParameterValue(DbParameter parameter, TypeUsage parameterType, object value)
        {
            Debug.Assert(parameter != null, "Validate parameter before calling SetParameterValue");
            Debug.Assert(parameterType != null, "Validate parameterType before calling SetParameterValue");

            SetDbParameterValue(parameter, parameterType, value);
        }

        protected virtual void SetDbParameterValue(DbParameter parameter, TypeUsage parameterType, object value)
        {
            Contract.Requires(parameter != null);
            Contract.Requires(parameterType != null);

            parameter.Value = value;
        }

        /// <summary>
        /// Create an instance of DbProviderServices based on the supplied DbConnection
        /// </summary>
        /// <param name="connection">The DbConnection to use</param>
        /// <returns>An instance of DbProviderServices</returns>
        public static DbProviderServices GetProviderServices(DbConnection connection)
        {
            return GetProviderServices(GetProviderFactory(connection));
        }

        internal static DbProviderFactory GetProviderFactory(string providerInvariantName)
        {
            Contract.Requires(providerInvariantName != null);
            DbProviderFactory factory;
            try
            {
                factory = DbProviderFactories.GetFactory(providerInvariantName);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(Strings.EntityClient_InvalidStoreProvider, e);
            }
            return factory;
        }

        /// <summary>
        /// Retrieve the DbProviderFactory based on the specified DbConnection
        /// </summary>
        /// <param name="connection">The DbConnection to use</param>
        /// <returns>An instance of DbProviderFactory</returns>
        public static DbProviderFactory GetProviderFactory(DbConnection connection)
        {
            Contract.Requires(connection != null);
            var factory = DbProviderFactories.GetFactory(connection);
            if (factory == null)
            {
                throw new ProviderIncompatibleException(Strings.EntityClient_ReturnedNullOnProviderMethod(
                    "get_ProviderFactory",
                    connection.GetType().ToString()));
            }
            Debug.Assert(factory != null, "Should have thrown on null");
            return factory;
        }

        internal static DbProviderServices GetProviderServices(DbProviderFactory factory)
        {
            Contract.Requires(factory != null);

            // TODO
            // This is where the EF provider is returned. It is here that the initial changes
            // to look-up the registered provider will be made. For now we are just loading the
            // SQL Server provider by Reflection. We use Reflection because EF.dll does not have
            // a reference to EF.SqlServer.dll.

            if (factory is SqlClientFactory)
            {
                var sqlProviderServicesType = Type.GetType(
                    "System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                    throwOnError: true);

                return (DbProviderServices)sqlProviderServicesType.GetProperty("SingletonInstance").GetValue(null);
            }

            if (factory is EntityProviderFactory)
            {
                return EntityProviderServices.Instance;
            }

            throw new NotSupportedException("TODO: Only the SQL Server provider is currently supported.");
        }

        /// <summary>
        /// Return an XML reader which represents the CSDL description
        /// </summary>
        /// <returns>An XmlReader that represents the CSDL description</returns>
        internal static XmlReader GetConceptualSchemaDefinition(string csdlName)
        {
            return GetXmlResource("System.Data.Resources.DbProviderServices." + csdlName + ".csdl");
        }

        internal static XmlReader GetXmlResource(string resourceName)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var stream = executingAssembly.GetManifestResourceStream(resourceName);
            return XmlReader.Create(stream, null, resourceName);
        }

        /// <summary>
        /// Generates a DDL script which creates schema objects (tables, primary keys, foreign keys) 
        /// based on the contents of the storeItemCollection and targeted for the version of the backend corresponding to 
        /// the providerManifestToken.
        /// Individual statements should be separated using database-specific DDL command separator. 
        /// It is expected that the generated script would be executed in the context of existing database with 
        /// sufficient permissions, and it should not include commands to create the database, but it may include 
        /// commands to create schemas and other auxiliary objects such as sequences, etc.
        /// </summary>
        /// <param name="providerManifestToken">The provider manifest token identifying the target version</param>
        /// <param name="storeItemCollection">The collection of all store items based on which the script should be created</param>
        /// <returns>
        /// A DDL script which creates schema objects based on contents of storeItemCollection 
        /// and targeted for the version of the backend corresponding to the providerManifestToken.
        /// </returns>
        public string CreateDatabaseScript(string providerManifestToken, StoreItemCollection storeItemCollection)
        {
            return DbCreateDatabaseScript(providerManifestToken, storeItemCollection);
        }

        protected virtual string DbCreateDatabaseScript(string providerManifestToken, StoreItemCollection storeItemCollection)
        {
            throw new ProviderIncompatibleException(Strings.ProviderDoesNotSupportCreateDatabaseScript);
        }

        /// <summary>
        /// Creates a database indicated by connection and creates schema objects 
        /// (tables, primary keys, foreign keys) based on the contents of storeItemCollection. 
        /// </summary>
        /// <param name="connection">Connection to a non-existent database that needs to be created 
        /// and be populated with the store objects indicated by the storeItemCollection</param>
        /// <param name="commandTimeout">Execution timeout for any commands needed to create the database.</param>
        /// <param name="storeItemCollection">The collection of all store items based on which the script should be created<</param>
        public void CreateDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            DbCreateDatabase(connection, commandTimeout, storeItemCollection);
        }

        protected virtual void DbCreateDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            throw new ProviderIncompatibleException(Strings.ProviderDoesNotSupportCreateDatabase);
        }

        /// <summary>
        /// Returns a value indicating whether given database exists on the server 
        /// and/or whether schema objects contained in teh storeItemCollection have been created.
        /// If the provider can deduct the database only based on the connection, they do not need
        /// to additionally verify all elements of the storeItemCollection.
        /// </summary>
        /// <param name="connection">Connection to a database whose existence is checked by this method</param>
        /// <param name="commandTimeout">Execution timeout for any commands needed to determine the existence of the database</param>
        /// <param name="storeItemCollection">The collection of all store items contained in the database 
        /// whose existence is determined by this method<</param>
        /// <returns>Whether the database indicated by the connection and the storeItemCollection exist</returns>
        public bool DatabaseExists(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            return DbDatabaseExists(connection, commandTimeout, storeItemCollection);
        }

        protected virtual bool DbDatabaseExists(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            throw new ProviderIncompatibleException(Strings.ProviderDoesNotSupportDatabaseExists);
        }

        /// <summary>
        /// Deletes all store objects specified in the store item collection from the database and the database itself.
        /// </summary>
        /// <param name="connection">Connection to an existing database that needs to be deleted</param>
        /// <param name="commandTimeout">Execution timeout for any commands needed to delete the database</param>
        /// <param name="storeItemCollection">The collection of all store items contained in the database that should be deleted<</param>
        public void DeleteDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            DbDeleteDatabase(connection, commandTimeout, storeItemCollection);
        }

        protected virtual void DbDeleteDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            throw new ProviderIncompatibleException(Strings.ProviderDoesNotSupportDeleteDatabase);
        }
    }
}
