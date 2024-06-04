using System;

namespace Revit.Data
{
    /// <summary>
    /// Eindeutiger Identifier für Planarfaces, kombinierter Schlüssel aus 3 einzelnen Ids, Equality nur mit Object und FaceId
    /// </summary>
    /// <seealso cref="IEquatable{Id}"/>
    public readonly struct Id : IEquatable<Id>
    {
        /// <summary>Id für Status (z.B. Phase, CalculationId, RegistrationID ...)</summary>
        public string StateId { get; }

        /// <summary>Id des Objektes, bei Scantra: <c>GlobalPlaneID</c> als Text bzw. die <c>StationID</c> des lokalen Patches</summary>
        public string ObjectId { get; }

        /// <summary>Id des Faces bei Scantra: entweder die <c>GlobalPlaneID</c> oder die lokale <c>ParameterID</c></summary>
        public string FaceId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id"/> struct.
        /// </summary>
        /// <param name="stateId"><see cref="StateId"/></param>
        /// <param name="objectId"><see cref="ObjectId"/></param>
        /// <param name="faceId"><see cref="FaceId"/></param>
        public Id(string stateId, string objectId, string faceId)
        {
            StateId = stateId;
            ObjectId = objectId ?? string.Empty;
            FaceId = faceId;
        }

        bool IEquatable<Id>.Equals(Id other)
        {
            throw new NotImplementedException();
        }
    }
}
