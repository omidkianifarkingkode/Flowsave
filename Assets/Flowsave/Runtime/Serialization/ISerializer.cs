using Flowsave.Configurations;

namespace FlowSave
{
    /// <summary>
    /// Defines serialization behavior for FlowSave contexts.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Gets a human-readable identifier for the serializer format.
        /// </summary>
        SerializerType Format { get; }

        /// <summary>
        /// Serializes the provided instance into a binary payload.
        /// </summary>
        /// <typeparam name="T">Type of the instance to serialize.</typeparam>
        /// <param name="data">Instance to serialize.</param>
        /// <returns>Binary payload representing the instance.</returns>
        byte[] Serialize<T>(T data);

        /// <summary>
        /// Deserializes the provided payload into an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="data">Binary payload to deserialize.</param>
        /// <returns>Instance of <typeparamref name="T"/> reconstructed from the payload.</returns>
        T Deserialize<T>(byte[] data);
    }
}
