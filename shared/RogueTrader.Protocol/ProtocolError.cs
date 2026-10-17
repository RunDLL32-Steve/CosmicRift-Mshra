namespace RogueTrader.Protocol;

/// <summary>Defined failure reasons. Decoding never reports failure any other way.</summary>
public enum ProtocolError
{
    None = 0,
    /// <summary>Frame or payload ended before a required field.</summary>
    Truncated,
    /// <summary>Version byte is not <see cref="ProtocolLimits.Version"/>.</summary>
    UnsupportedVersion,
    UnknownMessageType,
    /// <summary>Declared payload length exceeds <see cref="ProtocolLimits.MaxPayloadBytes"/>.</summary>
    PayloadTooLarge,
    /// <summary>Bytes remain after the declared payload or after the last field.</summary>
    TrailingBytes,
    /// <summary>A float field is NaN or infinite.</summary>
    NonFiniteValue,
    /// <summary>Enum byte is not a defined value.</summary>
    InvalidEnumValue,
    /// <summary>Reserved flag bits are set.</summary>
    InvalidFlags,
    TooManyEntities,
    StringTooLong,
    /// <summary>String is empty where a value is required, or is not valid UTF-8.</summary>
    InvalidString,
    /// <summary>Encoder destination is too small, or a message object is null.</summary>
    InvalidArgument,
}

/// <summary>The only exception the throwing convenience wrappers raise.</summary>
public sealed class ProtocolException : Exception
{
    public ProtocolError Error { get; }
    public ProtocolException(ProtocolError error) : base($"Protocol error: {error}") => Error = error;
}
