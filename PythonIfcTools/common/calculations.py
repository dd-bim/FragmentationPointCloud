import math

def crossFromAxis(axis1, axis2):
    x = axis1[1] * axis2[2] - axis1[2] * axis2[1]
    y = axis1[2] * axis2[0] - axis1[0] * axis2[2]
    z = axis1[0] * axis2[1] - axis1[1] * axis2[0]

    return tuple([x, y, z])


def getDirectionVector(pt1, pt2):
    x = pt2[0] - pt1[0]
    y = pt2[1] - pt1[1]
    z = pt2[2] - pt1[2]

    return tuple([x, y, z])


class Quaternion:

    def __init__(self, xAxis, yAxis, zAxis):
        self.S = math.sqrt(max(0.0, 1.0 + xAxis[0] + yAxis[1] + zAxis[2])) / 2.0

        x = math.sqrt(max(0.0, 1.0 + xAxis[0] - yAxis[1] - zAxis[2])) / 2.0
        y = math.sqrt(max(0.0, 1.0 - xAxis[0] + yAxis[1] - zAxis[2])) / 2.0
        z = math.sqrt(max(0.0, 1.0 - xAxis[0] - yAxis[1] + zAxis[2])) / 2.0

        self.X = -x if yAxis[2] < zAxis[1] else x
        self.Y = -y if zAxis[0] < xAxis[2] else y
        self.Z = -z if xAxis[1] < yAxis[0] else z

    def fromValues(self, s, x, y, z):
        self.S = s
        self.X = x
        self.Y = y
        self.Z = z

    def multiplyWithQuat(self, quat2):
        s = (-self.X * quat2.X) - (self.Y * quat2.Y) - (self.Z * quat2.Z) + (self.S * quat2.S)
        x = (self.X * quat2.S) + (self.Y * quat2.Z) - (self.Z * quat2.Y) + (self.S * quat2.X)
        y = (-self.X * quat2.Z) + (self.Y * quat2.S) + (self.Z * quat2.X) + (self.S * quat2.Y)
        z = (self.X * quat2.Y) - (self.Y * quat2.X) + (self.Z * quat2.S) + (self.S * quat2.Z)

        result = Quaternion([1, 0, 0], [0, 1, 0], [0, 0, 1])
        result.fromValues(s, x, y, z)

        return result

    def multiplyWithVector(self, vector):
        abX = (self.Y * vector[2]) - (self.Z * vector[1])
        abY = (self.Z * vector[0]) - (self.X * vector[2])
        abZ = (self.X * vector[1]) - (self.Y * vector[0])

        x = vector[0] + (2.0 * ((self.S * abX) + (self.Y * abZ) - (self.Z * abY)))
        y = vector[1] + (2.0 * ((self.S * abY) + (self.Z * abX) - (self.X * abZ)))
        z = vector[2] + (2.0 * ((self.S * abZ) + (self.X * abY) - (self.Y * abX)))

        return tuple([x, y, z])

    def getZAxis(self):
        xx = self.X * self.X
        yy = self.Y * self.Y
        s = 2.0 / ((self.S * self.S) + xx + yy + (self.Z * self.Z))

        x = s * ((self.S * self.Y) + (self.Z * self.X))
        y = s * ((self.Z * self.Y) - (self.S * self.X))
        z = 1.0 - (s * (xx + yy))

        return tuple([x, y, z])

    def getXAxis(self):
        yy = self.Y * self.Y
        zz = self.Z * self.Z
        s = 2.0 / ((self.S * self.S) + (self.X * self.X) + yy + zz)

        x = 1.0 - (s * (yy + zz))
        y = s * ((self.S * self.Z) + (self.X * self.Y))
        z = s * ((self.X * self.Z) - (self.S * self.Y))

        return tuple([x, y, z])

