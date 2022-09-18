using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultGPUInstance
{
    public class ObjectGenerate : MonoBehaviour
    {
        public GameObject prefab;
        [Range(0, 20000)]public int num = 0;
        private int curRestNum;
        public float range = 10.0f;

        private void Start() 
        {
            if(prefab == null) return;
            curRestNum = num;
            StartCoroutine("GenerateObjs");
        }

        IEnumerator GenerateObjs()
        {
            //分步加载
            int groupIdealSize = 50;
            while (curRestNum > 0)
            {
                int groupSize = Mathf.Min(groupIdealSize, curRestNum);
                for (int i = 0; i < groupSize; i++)
                {
                    Vector3 pos = Random.insideUnitSphere * range;
                    GameObject g = Instantiate<GameObject>(prefab, pos, Quaternion.identity);
                    g.transform.SetParent(transform);
                } 
                curRestNum -= groupIdealSize;
                yield return new WaitForFixedUpdate();
            }
        }
    }
}

