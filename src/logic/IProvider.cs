﻿namespace core;

/// <summary>
/// Attempts to provide the <typeparamref name="TService"/>.
/// </summary>
/// <typeparam name="TService">The service that may be provided.</typeparam>
public interface IProvider<TService>
{
    /// <summary>
    /// Attempt to provide the <typeparamref name="TService"/>.
    /// </summary>
    /// <param name="provider">The dependency injection container.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>
    /// An instance of <typeparamref name="TService"/>, or, null if the
    /// provider is not currently active in the <paramref name="configuration"/>.
    /// </returns>
    TService GetOrNull(IServiceProvider provider, IConfiguration configuration);
}