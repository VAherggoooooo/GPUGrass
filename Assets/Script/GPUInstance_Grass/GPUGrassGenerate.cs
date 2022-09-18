using UnityEngine;

namespace GPUInstanceGrass_Static
{
    [ExecuteAlways]
    public class GPUGrassGenerate : MonoBehaviour
    {
        [SerializeField] private Mesh m_grassMesh;//草mesh
        [SerializeField] private Material m_grassMat;//草material
        [SerializeField] private int m_grassRes = 150;//方形范围内, 草数量单边长
        [SerializeField] private ComputeShader m_cullCompute;//用于做剔除的compute shader
        //[SerializeField]
        private int m_cachedGrassRes = -1;//当前已绘制的数量        
        private Bounds m_totalBox = new Bounds(Vector3.zero, new Vector3(11.0f, 11.0f, 11.0f));//整体的包围盒, 影响整体的视锥体裁剪
        private ComputeBuffer m_posBuf;//储存位置的buffer
        private ComputeBuffer m_visiPosBuf;//储存可见位置的buffer
        private ComputeBuffer m_argsBuf;//mesh数据buffer
        private uint[] m_args = new uint[5]{0, 0, 0, 0, 0};//为m_argsBuf填充一些模型数据
        private Camera m_cam;

        //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

        /// <summary>
        /// 每帧更新buffer所需数据
        /// </summary>
        private void UpdateBuf()
        {
            if(m_grassRes <= 0) return;
            int t_grassCount = m_grassRes * m_grassRes;//总数量
            m_posBuf?.Release();
            m_posBuf = new ComputeBuffer(t_grassCount, sizeof(float) * 3);//开辟内存的类型与shader对应buf类型匹配
            m_visiPosBuf?.Release();        
            m_visiPosBuf = new ComputeBuffer(t_grassCount, sizeof(float) * 3, ComputeBufferType.Append);

            //开辟1块buffer, 大小m_args.Length * sizeof(uint), IndirectArguments模式(用于GPU绘制mesh)
            m_argsBuf?.Release();
            m_argsBuf = new ComputeBuffer(1, m_args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            //设置位置
            SetPoses();

            //网格数据
            if(m_grassMesh != null)
            {
                //mesh数据
                m_args[0] = (uint)m_grassMesh.GetIndexCount(0);
                m_args[1] = (uint)t_grassCount;
                m_args[2] = (uint)m_grassMesh.GetIndexStart(0);
                m_args[3] = (uint)m_grassMesh.GetBaseVertex(0);
            }
            else
            {
                m_args[0] = m_args[1] = m_args[2] = m_args[3] = 0;
            }
            m_argsBuf.SetData(m_args);//mesh数据传入buffer, buffer在DrawMeshInstancedIndirect使用        
            m_cachedGrassRes = t_grassCount;
        }
        private void SetPoses()
        {
            int t_grassCount = m_grassRes * m_grassRes;//数量
            Vector3[] t_poses = new Vector3[t_grassCount];

            // >>>>>>>>>>>>>>>>>>>>>> 填充位置 <<<<<<<<<<<<<<<<<<<<<<<<<<
            float t_grdSize = 10.0f;//地面大小
            float t_stp = t_grdSize / (float)m_grassRes;//间距

            Vector3 t_startPos = new Vector3(-t_grdSize, 0, -t_grdSize) * 0.5f;
            for (int i = 0; i < m_grassRes; i++)//示例: 方形区域
            {
                for (int j = 0; j < m_grassRes; j++)
                {
                    t_poses[i * m_grassRes + j] = new Vector3(i * t_stp, 0, j * t_stp) + t_startPos;//设置草位置
                }
            }
            // >>>>>>>>>>>>>>>>>>>>>>  <<<<<<<<<<<<<<<<<<<<<<<<<<

            m_posBuf.SetData(t_poses);//将坐标传入buffer
        }
        private void ReleaseBuffer(ref ComputeBuffer _buf)
        {
            if(_buf == null) return;
            _buf?.Release();
            _buf = null;
        }

        //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

        private void OnEnable() 
        {
            if(m_cam == null) m_cam = Camera.main;
            if(m_cam == null)
            {
                enabled = false;
                return;
            }
            UpdateBuf();
        }
        private void OnDisable() 
        {
            ReleaseBuffer(ref m_posBuf);
            ReleaseBuffer(ref m_visiPosBuf);
            ReleaseBuffer(ref m_argsBuf);
        }
        private void Update() 
        {
            if(m_grassRes <= 0) return;
            if(m_grassRes * m_grassRes != m_cachedGrassRes || /* 当前已绘制的数量 != 输入参数数量 */
                m_argsBuf == null ||
                m_posBuf == null ||
                m_visiPosBuf == null)        
            {
                //更新buffer
                UpdateBuf();       
            }  

            //computeshader 进行剔除
            Matrix4x4 v = m_cam.worldToCameraMatrix;
            Matrix4x4 p = m_cam.projectionMatrix;
            Matrix4x4 vp = p * v;
            int kernel = m_cullCompute.FindKernel("CSMain");
            m_visiPosBuf.SetCounterValue(0);//可见buf设置为0, 经过GPU剔除后计算值
            m_cullCompute.SetMatrix("_VPMatrix", vp);
            m_cullCompute.SetBuffer(kernel, "positionBuffer", m_posBuf);//buf传入computeshader进行cull
            m_cullCompute.SetBuffer(kernel, "visiPosBuffer", m_visiPosBuf);//buf传入computeshader进行cull
            m_cullCompute.Dispatch(kernel, Mathf.Max(m_cachedGrassRes / 64, 1), 1, 1);        

            m_grassMat.SetBuffer("positionBuffer", m_visiPosBuf);//剔除后的位置buffer传入mat
            ComputeBuffer.CopyCount(m_visiPosBuf, m_argsBuf, sizeof(uint));//buf拷贝
            Graphics.DrawMeshInstancedIndirect(m_grassMesh, 0, m_grassMat, m_totalBox, m_argsBuf);//每帧绘制
        }
    }
}
