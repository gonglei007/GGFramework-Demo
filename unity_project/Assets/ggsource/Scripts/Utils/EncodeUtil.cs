using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EncodeUtil {

    static int keyOffset = 0;
    public static Byte[] XorEncryptionData(Byte[] data, string key)
    {
        Byte[] keyBuffer = null;
        if (key != null && key != "") { 
            keyBuffer = Encoding.ASCII.GetBytes(key);
        }
        return XorEncryptionData(data, keyBuffer);
    }

    /**
        * XOR加密數據
        */
    public static Byte[] XorEncryptionData(Byte[] data, Byte[] key) {
        if (data == null || data.Length <= 0) {
            return data;
        }
        if (key == null || key.Length <= 0) {
            return data;
        }
        Byte[] result = new Byte[data.Length];
        int keyLength = key.Length;
        for(int offset=0; offset< data.Length; ++offset){
            result[offset] = (Byte)(data[offset] ^ key[keyOffset]);
            keyOffset = (keyOffset + 1) % keyLength;
        }
        return result;
    }
}
