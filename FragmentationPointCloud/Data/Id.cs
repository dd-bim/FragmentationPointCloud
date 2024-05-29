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

        public int PartId { get; } = 0;

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
            int partIdx = faceId.LastIndexOf('_');
            if (partIdx < 0)
                FaceId = faceId;
            else
            {
                FaceId = faceId.Substring(0, partIdx);
                PartId = Int32.TryParse(faceId.Substring(partIdx + 1), out int partId) ? partId : 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id"/> struct.
        /// </summary>
        /// <param name="createdId"></param>
        /// <param name="demolishedId"></param>
        /// <param name="objectId"><see cref="ObjectId"/></param>
        /// <param name="faceId"><see cref="FaceId"/></param>
        public Id(string createdId, string demolishedId, string objectId, string faceId)
        {
            StateId = string.IsNullOrWhiteSpace(demolishedId) ? createdId : createdId + '|' + demolishedId;
            ObjectId = objectId ?? string.Empty;
            int partIdx = faceId.LastIndexOf('_');
            if (partIdx < 0)
                FaceId = faceId;
            else
            {
                FaceId = faceId.Substring(0, partIdx);
                PartId = Int32.TryParse(faceId.Substring(partIdx + 1), out int partId) ? partId : 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id"/> struct.
        /// </summary>
        /// <param name="stateId"><see cref="StateId"/></param>
        /// <param name="objectId"><see cref="ObjectId"/></param>
        /// <param name="faceId"><see cref="FaceId"/></param>
        public Id(string stateId, string objectId, string faceId, int partId)
        {
            StateId = stateId;
            ObjectId = objectId ?? string.Empty;
            FaceId = faceId;
            PartId = partId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Id"/> struct.
        /// </summary>
        /// <param name="createdId"></param>
        /// <param name="demolishedId"></param>
        /// <param name="objectId"><see cref="ObjectId"/></param>
        /// <param name="faceId"><see cref="FaceId"/></param>
        public Id(string createdId, string demolishedId, string objectId, string faceId, int partId)
        {
            StateId = string.IsNullOrWhiteSpace(demolishedId) ? createdId : createdId + '|' + demolishedId;
            ObjectId = objectId ?? string.Empty;
            FaceId = faceId;
            PartId = partId;
        }

        public string CreatedId
        {
            get
            {
                var i = StateId.IndexOf('|');
                return i < 0 ? StateId : StateId.Substring(0, i);
            }
        }

        public string DemolishedId
        {
            get
            {
                var i = StateId.IndexOf('|');
                return i < 0 ? "" : StateId.Substring(i + 1);
            }
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise, <see langword="false" />.
        /// </returns>
        public override bool Equals(object? obj)
        {
            return obj is Id face && Equals(face);
        }

        public bool Equals(Id other)
        {
            return ObjectId == other.ObjectId &&
                   FaceId == other.FaceId && PartId == other.PartId;
        }

        public override int GetHashCode()
        {
#if NETSTANDARD2_0 || NET472 || NET48
            return ObjectId.GetHashCode() ^ FaceId.GetHashCode() ^ PartId;
#else
            return HashCode.Combine(ObjectId, FaceId, PartId);
#endif
        }

        public static bool operator ==(Id left, Id right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Id left, Id right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// The fully qualified type name.
        /// </returns>
        public override string? ToString()
        {
            return PartId == 0
                ? $"{StateId};{ObjectId};{FaceId}"
                : $"{StateId};{ObjectId};{FaceId}_{PartId}";
        }
    }
}
