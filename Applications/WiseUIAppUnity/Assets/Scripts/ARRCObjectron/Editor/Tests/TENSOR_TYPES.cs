using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ARRCObjectron
{
    public unsafe struct _TENSOR_ARRAY
    {
        public int count;
        public _TENSOR* tensor;

        public _TENSOR_ARRAY(_TENSOR[] tensor)
        {
            this.count = tensor.Length;
            fixed (_TENSOR* pTensor = tensor)
            {
                this.tensor = pTensor;
            }
        }

        public unsafe _TENSOR GetTensor(int idx)
        {
            _TENSOR output = new _TENSOR();
            output = tensor[idx];
            return output;
        }
    }

    public unsafe struct _TENSOR
    {
        public int n, h, w, c;
        public ulong elementCount;
        public float* data;

        public unsafe _TENSOR(int n, int h, int w, int c, float[] data)
        {
            this.n = n;
            this.h = h;
            this.w = w;
            this.c = c;
            elementCount = (uint)n * (uint)h * (uint)w * (uint)c;
            

            fixed (float* pData = data)
            {
                this.data = pData;
            }
        }
        public unsafe float[] DownloadData()
        {
            var output = new float[elementCount];
            for (ulong i = 0; i < elementCount; i++)
                output[i] = data[i];
            return output;
        }
    }
    #region obsoluted_managed_tensor
    [Obsolete]
    public struct _TENSOR_ARRAY_MANAGED
    {
        public int count;
        public IntPtr tensors; //어차피 managed안되는거 같아서 중단.

        public _TENSOR_ARRAY_MANAGED(_TENSOR_MANAGED[] newItem)
        {
            this.count = newItem.Length;
            int bufferSize = Marshal.SizeOf(newItem[0]);
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

            long longPtr = buffer.ToInt64(); // Must work both on x86 and x64
            for (int i = 0; i < newItem.Length; i++)
            {
                IntPtr rectPtr = new IntPtr(longPtr);
                Marshal.StructureToPtr(newItem[i], rectPtr, false); // You do not need to erase struct in this case
                longPtr += Marshal.SizeOf(typeof(_TENSOR_MANAGED));
            }

            this.tensors = buffer;
        }

    }
    [Obsolete]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct _TENSOR_MANAGED
    {
        [MarshalAs(UnmanagedType.U4)]
        public int n, h, w, c;

        [MarshalAs(UnmanagedType.U8)]
        public ulong elementCount;

        //IntPtr 마샬링 타입 모르겠음.
        //[MarshalAs(UnmanagedType.U8)] 
        //public IntPtr data;

        public _TENSOR_MANAGED(int n, int h, int w, int c, float[] data)
        {
            this.n = n;
            this.h = h;
            this.w = w;
            this.c = c;
            elementCount = (uint)n * (uint)h * (uint)w * (uint)c;

            int size = Marshal.SizeOf(data[0]) * data.Length;
            IntPtr pnt = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, 0, pnt, data.Length);
            //this.data = pnt;
        }

    }
    #endregion
}
