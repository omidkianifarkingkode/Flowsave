using FlowSave.Configurations;
using System;
using System.Threading.Tasks;

namespace FlowSave
{
    public interface IFlowSaveService
    {
        /// <summary>
        /// Saves data asynchronously for a specific namespace.
        /// </summary>
        /// <typeparam name="T">Type of the data to save.</typeparam>
        /// <param name="namespaceId">Identifier for the save context.</param>
        /// <param name="data">Data to save.</param>
        /// <returns>A task representing the save operation.</returns>
        Task SaveAsync<T>(string namespaceId, T data);

        /// <summary>
        /// Loads data asynchronously for a specific namespace.
        /// </summary>
        /// <typeparam name="T">Type of the data to load.</typeparam>
        /// <param name="namespaceId">Identifier for the load context.</param>
        /// <returns>A task representing the load operation, returning the loaded data.</returns>
        Task<T> LoadAsync<T>(string namespaceId);

        /// <summary>
        /// Checks if data exists for a specific namespace.
        /// </summary>
        /// <param name="namespaceId">Identifier for the save context.</param>
        /// <returns>True if data exists, false otherwise.</returns>
        Task<bool> HasSaveAsync(string namespaceId);

        /// <summary>
        /// Deletes data for a specific namespace.
        /// </summary>
        /// <param name="namespaceId">Identifier for the save context.</param>
        /// <returns>A task representing the delete operation.</returns>
        Task DeleteSaveAsync(string namespaceId);
    }
}

