namespace sRPC
{
    /// <summary>
    /// The interface that can provide a <typeparamref name="T"/> API.
    /// </summary>
    /// <typeparam name="T">the type of the API</typeparam>
    public interface IApi<T>
    {
        /// <summary>
        /// The current API interface.
        /// </summary>
        T Api { get; }
    }
}
