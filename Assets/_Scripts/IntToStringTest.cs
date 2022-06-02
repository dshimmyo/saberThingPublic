using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class IntToStringTest : MonoBehaviour {
    //public StaticString staticString;//this does not need to be a static
    StringBuilder sb = new StringBuilder(8,20);//using stringbuilder to speed up my cut point caching algo
    const int CHAR_0 = (int)'0';//const being used for stringbuilder

	// Use this for initialization
	void Start () 
    {
 
	}
	
//    public string SBIntToString(int myInt,int myPadding)
//    {
//        int log = (int)System.Math.Floor(System.Math.Log10(myInt));
//        for (int j=log+1; j<myPadding; j++){
//            sb.Append ((char)(CHAR_0));
//        }
//        for (int i = log; i >= 0; i--)
//        {
//            int pow = (int)System.Math.Pow(10,i);
//            int digit = (myInt / pow) % 10;
//            sb.Append ((char)(digit + CHAR_0));
//        }
//        return sb.ToString();
//    }
    public string SBIntsToString(int[] myInt, int myPadding)
    {
        sb.Length = 0;
        for (int k=0;k<myInt.Length;k++)
            {
            int log = (int)System.Math.Floor(System.Math.Log10(myInt[k]));
            for (int j=log+1; j<myPadding; j++){
                sb.Append ((char)(CHAR_0));
            }
            for (int i = log; i >= 0; i--)
            {
                int pow = (int)System.Math.Pow(10,i);
                int digit = (myInt[k] / pow) % 10;
                sb.Append ((char)(digit + CHAR_0));
            }
        }
        return sb.ToString();//try GatValue as string;
    }
	// Update is called once per frame
	void Update () {
        sb.Length = 0;
        int testInt1 = Random.Range(0,9999);
        int testInt2 = Random.Range(0,9999);
        int numRepeats = 10000;
        string testString="";
        if (Time.frameCount % 2 == 0)
        {  
            for (int i = 0; i<numRepeats; i++)
            {
            if (i==0)
                testString = SBIntsToString(new int[]{testInt1,testInt2},4);
                if (i==0)
                    Debug.Log("sb func: " + testString);

            }
        }
        else
        {
            for (int i = 0; i<numRepeats; i++)
            {
                testString = testInt1.ToString("0000")+testInt2.ToString("0000");
                if (i==0)
                    Debug.Log("int32ToString" + testString);
            }
        }

	}
}
