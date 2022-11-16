using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Matrix3x3
{
    //public Matrix3x3 Matrix3x3();
    //생성자
    //벡터 3개 넣었을 때
    //아무것도 없을 때
    //각 요소
    //로우별
    //곱하기
    //인버스
    public Vector3 row1, row2, row3;
    public Vector3 col1, col2, col3;
    public float m00, m01, m02;
    public float m10, m11, m12;
    public float m20, m21, m22;
    public Matrix3x3()
    {
        row1 = new Vector3(1f, 0f, 0f);
        row2 = new Vector3(0f, 1f, 0f);
        row3 = new Vector3(0f, 0f, 1f);
        col1 = new Vector3(1f, 0f, 0f);
        col2 = new Vector3(0f, 1f, 0f);
        col3 = new Vector3(0f, 0f, 1f);
        m00 = 1f; m01 = 0f; m02 = 0f;
        m10 = 0f; m11 = 1f; m12 = 0f;
        m20 = 0f; m21 = 0f; m22 = 1f;
    }
    
    override public string ToString()
    {
        return m00 + " " + m01 + " " + m02 + "\n" + m10 + " " + m11 + " " + m12 + "\n" + m20 + " " + m21 + " " + m22;
    }

    public Matrix3x3(Vector3 r1, Vector3 r2, Vector3 r3)
    {
        row1 = r1;
        row2 = r2;
        row3 = r3;
        m00 = r1.x; m01 = r1.y; m02 = r1.z;
        m10 = r2.x; m11 = r2.y; m12 = r2.z;
        m20 = r3.x; m21 = r3.y; m22 = r3.z;

        col1 = new Vector3(m00, m10, m20);
        col2 = new Vector3(m01, m11, m21);
        col3 = new Vector3(m02, m12, m22);

    }
    public Matrix3x3(float _m00, float _m01, float _m02, float _m10, float _m11, float _m12, float _m20, float _m21, float _m22)
    {
        m00 = _m00;
        m01 = _m01;
        m02 = _m02;
        m10 = _m10;
        m11 = _m11;
        m12 = _m12;
        m20 = _m20;
        m21 = _m21;
        m22 = _m22;
        row1 = new Vector3(m00, m01, m02);
        row2 = new Vector3(m10, m11, m12);
        row3 = new Vector3(m20, m21, m22);
        col1 = new Vector3(m00, m10, m20);
        col2 = new Vector3(m01, m11, m21);
        col3 = new Vector3(m02, m12, m22);
    }

    public void Copy(ref float[] data, int sIdx)
    {
        data[sIdx]   = m00;
        data[sIdx+1] = m01;
        data[sIdx+2] = m02;
        data[sIdx+3] = m10;
        data[sIdx+4] = m11;
        data[sIdx+5] = m12;
        data[sIdx+6] = m20;
        data[sIdx+7] = m21;
        data[sIdx+8] = m22;
    }

    //public Matrix3x3 ADD(Matrix3x3 mat) {
    //    return new Matrix3x3(row1 + mat.row1, row2 + mat.row2, row3 + mat.row3);
    //}
    public static Matrix3x3 EXP(Vector3 expCoord)
    {
        float rad = expCoord.magnitude;
        //Debug.Log("EXP::rad::" + rad);
        if (rad == 0f)
        {
            return new Matrix3x3();
        }
        else
        {
            Vector3 unitEXPCoord = expCoord.normalized;
            Matrix3x3 temp1 = Matrix3x3.SkewSymmetric(unitEXPCoord);
            Matrix3x3 temp2 = temp1 * temp1;
            return (new Matrix3x3() + (Mathf.Sin(rad)) * temp1 + (1f - Mathf.Cos(rad)) * temp2);

            //Matrix3x3 temp1 = Matrix3x3.SkewSymmetric(expCoord);
            //Matrix3x3 temp2 = temp1 * temp1;
            //return (new Matrix3x3() + (Mathf.Sin(rad) / rad) * temp1 + ((1f - Mathf.Cos(rad)) / (rad * rad)) * temp2);
        }
    }

    public static Matrix3x3 SkewSymmetric(Vector3 vec)
    {
        return new Matrix3x3(new Vector3(0f, -vec.z, vec.y), new Vector3(vec.z, 0f, -vec.x), new Vector3(-vec.y, vec.x, 0f));
    }

    public static Matrix3x3 Jacobian(Vector3 vec)
    {
        float theta = vec.magnitude;
        Matrix3x3 skewMat = Matrix3x3.SkewSymmetric(vec.normalized);
        Matrix3x3 res = new Matrix3x3()-(1-Mathf.Cos(theta))/theta*skewMat+(theta-Mathf.Sin(theta))/theta*skewMat*skewMat;

        return res;
    }

    public static Matrix3x3 InverseJacobian(Vector3 vec)
    {
        float theta = vec.magnitude;
        Matrix3x3 skewMat = Matrix3x3.SkewSymmetric(vec.normalized);
        Matrix3x3 skewMat2 = skewMat * skewMat;
        Matrix3x3 res = new Matrix3x3() + 0.5f * theta * skewMat + skewMat2 + theta * 0.5f*(1 + Mathf.Cos(theta)) / Mathf.Sin(theta) * skewMat2;
        return res;
    }
    //public Vector3 Multiply(Matrix3x3 mat, Vector3 vec) {
    //    return new Vector3(Vector3.Dot(mat.row1, vec), Vector3.Dot(mat.row2, vec), Vector3.Dot(mat.row3, vec));
    //}

    public static Matrix3x3 operator -(Matrix3x3 mat1, Matrix3x3 mat2) => new Matrix3x3(mat1.row1 - mat2.row1, mat1.row2 - mat2.row2, mat1.row3 - mat2.row3);
    public static Matrix3x3 operator +(Matrix3x3 mat1, Matrix3x3 mat2) => new Matrix3x3(mat1.row1 + mat2.row1, mat1.row2 + mat2.row2, mat1.row3 + mat2.row3);
    public static Matrix3x3 operator *(Matrix3x3 mat1, Matrix3x3 mat2) => new Matrix3x3(
        Vector3.Dot(mat1.row1, mat2.col1), Vector3.Dot(mat1.row1, mat2.col2), Vector3.Dot(mat1.row1, mat2.col3),
        Vector3.Dot(mat1.row2, mat2.col1), Vector3.Dot(mat1.row2, mat2.col2), Vector3.Dot(mat1.row2, mat2.col3),
        Vector3.Dot(mat1.row3, mat2.col1), Vector3.Dot(mat1.row3, mat2.col2), Vector3.Dot(mat1.row3, mat2.col3));
    public static Vector3 operator *(Matrix3x3 mat, Vector3 vec) => new Vector3(Vector3.Dot(mat.row1, vec), Vector3.Dot(mat.row2, vec), Vector3.Dot(mat.row3, vec));
    public static Vector3 operator *(Vector3 vec, Matrix3x3 mat) => new Vector3(Vector3.Dot(mat.col1, vec), Vector3.Dot(mat.col2, vec), Vector3.Dot(mat.col3, vec));
    public static Matrix3x3 operator *(float f, Matrix3x3 mat) => new Matrix3x3(f * mat.row1, f * mat.row2, f * mat.row3);
    public static Matrix3x3 operator *(Matrix3x3 mat, float f) => new Matrix3x3(f * mat.row1, f * mat.row2, f * mat.row3);
    //public static Matrix3x3 operator =(Matrix3x3 mat) => new Matrix3x3(mat.row1, mat.row2, mat.row3);
    public Matrix3x3 Transpose()
    {
        return new Matrix3x3(new Vector3(m00, m10, m20), new Vector3(m01, m11, m21), new Vector3(m02, m12, m22));
    }
    public Vector3 LOG()
    {
        Vector3 res = Vector3.zero;
        if (m00 == 1f && m11 == 1f && m22 == 1f)
        {
        }
        else
        {
            float traceR = m00 + m11 + m22;
            float theta = Mathf.Acos((traceR - 1f) / 2f);
            //Debug.Log("log::theta::" + theta + "," + theta * Mathf.Rad2Deg+"::trace::"+traceR);
            float temp = Mathf.Sin(theta);
            if (Mathf.Abs(theta) < 0.00001) {
                return res;
            }
            
            temp = theta / (2f * temp);
            res.x = m21 - m12;
            res.y = m02 - m20;
            res.z = m10 - m01;
            res *= temp;
        }
        if (float.IsNaN(res.magnitude))
            return Vector3.zero;
        return res;
    }

    public Vector3 Orientation()
    {
        float z = Mathf.Atan2(m01, m11);
        float x = Mathf.Asin(-m21);
        float y = Mathf.Atan2(-m20, m22);
        return new Vector3(x,y,z);
    }
    public Vector3 OrientationZXY()
    {
        float z = Mathf.Atan2(-m01, m11);
        float x = Mathf.Asin(m21);
        float y = Mathf.Atan2(-m20, m22);
        return new Vector3(x, y, z);
    }

    public static Matrix3x3 EulerToRot(Vector3 vec)
    {
        //XYZ Euler angle

        //Matrix3x3 res = new
        //degree -> radian, radian으로 계산해야 함.
        //cos, sin input : radian
        
        float cx = Mathf.Cos(vec.x);
        float sx = Mathf.Sin(vec.x);

        float cy = Mathf.Cos(vec.y);
        float sy = Mathf.Sin(vec.y);

        float cz = Mathf.Cos(vec.z);
        float sz = Mathf.Sin(vec.z);

        Matrix3x3 xaxis = new Matrix3x3(1f, 0f, 0f, 0f, cx, -sx, 0f, sx, cx);
        //xaxis.m00 = 1.0f;
        //xaxis.m11 = cx;
        //xaxis.m12 = -sx;
        //xaxis.m21 = sx;
        //xaxis.m22 = cx;
        
        Matrix3x3 yaxis = new Matrix3x3(cy, 0f, sy, 0f, 1f, 0f, -sy, 0f, cy);
        //yaxis.m00 = cy;
        //yaxis.m02 = sy;
        //yaxis.m11 = 1.0f;
        //yaxis.m20 = -sy;
        //yaxis.m22 = cy;

        Matrix3x3 zaxis = new Matrix3x3(cz, -sz, 0f, sz, cz, 0f, 0f, 0f, 1f);
        //zaxis.m00 = cz;
        //zaxis.m01 = -sz;
        //zaxis.m10 = sz;
        //zaxis.m11 = cz;
        //zaxis.m22 = 1.0f;

        //intrinsic yxz
        //Matrix3x3 res = zaxis*xaxis*yaxis;
        //Matrix3x3 res = yaxis * xaxis * zaxis;

        //ZYX
        Matrix3x3 res = zaxis * yaxis * xaxis;

        //Debug.Log(res.ToString());
        CheckMatEpsilon(ref res);
        
        return res;

    }
    static void CheckMatEpsilon(ref Matrix3x3 mat)
    {
        mat.m00 = CheckEpsilon(mat.m00);
        mat.m01 = CheckEpsilon(mat.m01);
        mat.m02 = CheckEpsilon(mat.m02);
        mat.m10 = CheckEpsilon(mat.m10);
        mat.m11 = CheckEpsilon(mat.m11);
        mat.m12 = CheckEpsilon(mat.m12);
        mat.m20 = CheckEpsilon(mat.m20);
        mat.m21 = CheckEpsilon(mat.m21);
        mat.m22 = CheckEpsilon(mat.m22);
    }
    static float CheckEpsilon(float val)
    {
        if (Mathf.Abs(val) < 0.00001f)
            return 0.0f;
        else
            return val;
    }

    //https://d3cw3dd2w32x2b.cloudfront.net/wp-content/uploads/2015/01/matrix-to-quat.pdf
    public static Quaternion RotToQuar(Matrix3x3 mat)
    {
        Quaternion q = Quaternion.identity;
        float t = 0f;
        if (mat.m22 < 0)
        {
            if (mat.m00 > mat.m11)
            {
                t = 1 + mat.m00 - mat.m11 - mat.m22;
                q = new Quaternion(t, mat.m01 + mat.m10, mat.m20 + mat.m02, mat.m12 - mat.m21);
            }
            else
            {
                t = 1 - mat.m00 + mat.m11 - mat.m22;
                q = new Quaternion(mat.m01 + mat.m10, t, mat.m12 + mat.m21, mat.m20 - mat.m02);
            }
        }
        else
        {
            if (mat.m00 < -mat.m11)
            {
                t = 1 - mat.m00 - mat.m11 + mat.m22;
                q = new Quaternion(mat.m20 + mat.m02, mat.m12 + mat.m21, t, mat.m01 - mat.m10);
            }
            else
            {
                t = 1 + mat.m00 + mat.m11 + mat.m22;
                q = new Quaternion(mat.m12 - mat.m21, mat.m20 - mat.m02, mat.m01 - mat.m10, t);
            }
        }
        float fscale = 0.5f / Mathf.Sqrt(t);
        q.x *= fscale;
        q.y *= fscale;
        q.z *= fscale;
        q.w *= fscale;

        return q;
    }

}