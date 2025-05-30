using System;

namespace Revit.Data;

/// <summary>
/// Represents a unique identifier for planar faces, composed of a combination of state, object, and face identifiers.
/// </summary>
/// <remarks>The <see cref="Id"/> struct is designed to uniquely identify planar faces in a system by combining
/// multiple identifiers: <list type="bullet"> <item><description><see cref="StateId"/>: Represents the state of the
/// object (e.g., phase, calculation ID, registration ID).</description></item> <item><description><see
/// cref="ObjectId"/>: Represents the object identifier, such as a global plane ID or a station ID for local
/// patches.</description></item> <item><description><see cref="FaceId"/>: Represents the face identifier, which may
/// include a global plane ID or a local parameter ID.</description></item> <item><description><see cref="PartId"/>:
/// Represents an optional part identifier extracted from the face ID, defaulting to 0 if not
/// present.</description></item> </list> This struct implements <see cref="IEquatable{Id}"/> to allow for equality
/// comparisons based on the <see cref="ObjectId"/>, <see cref="FaceId"/>, and <see cref="PartId"/>
/// properties.</remarks>
public readonly record struct Id : IEquatable<Id>
{
    /// <summary>Id für Status (z.B. Phase, CalculationId, RegistrationID ...)</summary>
    public string StateId { get; }

    /// <summary>Id des Objektes, bei Scantra: <c>GlobalPlaneID</c> als Text bzw. die <c>StationID</c> des lokalen Patches</summary>
    public string ObjectId { get; }

    /// <summary>Id des Faces bei Scantra: entweder die <c>GlobalPlaneID</c> oder die lokale <c>ParameterID</c></summary>
    public string FaceId { get; }

    /// <summary>
    /// Gets the unique identifier for the part.
    /// </summary>
    public int PartId { get; init; } = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="Id"/> class with the specified state, object, and face identifiers.
    /// </summary>
    /// <remarks>The constructor parses the <paramref name="faceId"/> to extract the face and part
    /// identifiers. If the <paramref name="faceId"/> contains an underscore ('_'), the portion before the underscore is
    /// treated as the face identifier, and the portion after the underscore is parsed as the part identifier. If
    /// parsing fails, the part identifier defaults to 0.</remarks>
    /// <param name="stateId">The identifier for the state. This value cannot be null or empty.</param>
    /// <param name="objectId">The identifier for the object. This value cannot be null or empty.</param>
    /// <param name="faceId">The identifier for the face, which may optionally include a part identifier appended with an underscore. For
    /// example, "face_1" will set the face identifier to "face" and the part identifier to 1. If no underscore is
    /// present, the entire value is treated as the face identifier, and the part identifier is set to 0.</param>
    public Id(string stateId, string objectId, string faceId)
    {
        StateId = stateId;
        ObjectId = objectId;
        int partIdx = faceId.LastIndexOf('_');
        if (partIdx < 0)
        {
            FaceId = faceId;
        }
        else
        {
            FaceId = faceId[..partIdx];
            PartId = int.TryParse(faceId[(partIdx + 1)..], out int partId) ? partId : 0;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id"/> class with the specified identifiers.
    /// </summary>
    /// <remarks>The <see cref="StateId"/> is constructed by combining the <paramref name="createdId"/> and
    /// <paramref name="demolishedId"/>  with a pipe ('|') separator if both are provided. If <paramref
    /// name="demolishedId"/> is null or whitespace,  the <see cref="StateId"/> is set to the value of <paramref
    /// name="createdId"/> alone.</remarks>
    /// <param name="createdId">The identifier for the created state. This value is always included in the <see cref="StateId"/>.</param>
    /// <param name="demolishedId">The identifier for the demolished state. If provided, it is appended to the <paramref name="createdId"/> in the
    /// <see cref="StateId"/>.</param>
    /// <param name="objectId">The identifier for the associated object.</param>
    /// <param name="faceId">The identifier for the face, which may include an optional part index.  If the <paramref name="faceId"/>
    /// contains an underscore ('_'), the portion before the underscore is used as the <see cref="FaceId"/>,  and the
    /// portion after the underscore is parsed as the <see cref="PartId"/>. If parsing fails, <see cref="PartId"/> is
    /// set to 0.</param>
    public Id(string createdId, string demolishedId, string objectId, string faceId)
    {
        StateId = string.IsNullOrWhiteSpace(demolishedId) ? createdId : createdId + '|' + demolishedId;
        ObjectId = objectId;
        int partIdx = faceId.LastIndexOf('_');
        if (partIdx < 0)
        {
            FaceId = faceId;
        }
        else
        {
            FaceId = faceId[..partIdx];
            PartId = int.TryParse(faceId[(partIdx + 1)..], out int partId) ? partId : 0;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id"/> class with the specified identifiers.
    /// </summary>
    /// <param name="stateId">The identifier representing the state. Cannot be null or empty.</param>
    /// <param name="objectId">The identifier representing the object. Cannot be null or empty.</param>
    /// <param name="faceId">The identifier representing the face. Cannot be null or empty.</param>
    /// <param name="partId">The identifier representing the part. Must be a non-negative integer.</param>
   public Id(string stateId, string objectId, string faceId, int partId)
    {
        StateId = stateId;
        ObjectId = objectId;
        FaceId = faceId;
        PartId = partId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Id"/> class with the specified identifiers.
    /// </summary>
    /// <remarks>The <see cref="StateId"/> is constructed by combining <paramref name="createdId"/> and
    /// <paramref name="demolishedId"/> with a '|' separator if <paramref name="demolishedId"/> is not null or
    /// whitespace.</remarks>
    /// <param name="createdId">The identifier for the created state. Cannot be null or whitespace.</param>
    /// <param name="demolishedId">The identifier for the demolished state. If null or whitespace, only <paramref name="createdId"/> is used to
    /// construct the <see cref="StateId"/>.</param>
    /// <param name="objectId">The identifier for the associated object. Cannot be null or whitespace.</param>
    /// <param name="faceId">The identifier for the associated face. Cannot be null or whitespace.</param>
    /// <param name="partId">The identifier for the associated part.</param>
    public Id(string createdId, string demolishedId, string objectId, string faceId, int partId)
    {
        StateId = string.IsNullOrWhiteSpace(demolishedId) ? createdId : createdId + '|' + demolishedId;
        ObjectId = objectId;
        FaceId = faceId;
        PartId = partId;
    }

    public bool Equals(Id other)
    {
        return ObjectId == other.ObjectId &&
               FaceId == other.FaceId && PartId == other.PartId;
    }

    public override int GetHashCode() => HashCode.Combine(ObjectId, FaceId, PartId);

    /// <summary>
    /// Returns a string representation of the object, including its state, object, face, and part identifiers.
    /// </summary>
    /// <returns>A string in the format "StateId;ObjectId;FaceId" if <see cref="PartId"/> is 0;  otherwise,
    /// "StateId;ObjectId;FaceId_PartId".</returns>
    public override string ToString()
    {
        return PartId == 0
            ? $"{StateId};{ObjectId};{FaceId}"
            : $"{StateId};{ObjectId};{FaceId}_{PartId}";
    }
}