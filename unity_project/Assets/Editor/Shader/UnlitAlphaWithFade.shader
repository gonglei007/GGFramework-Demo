Shader "GO/UnlitAlphaWithFade"
{
	//-------------------------------【属性】------------------------------------
	Properties
	{
		_MainTex("基础纹理(RGBA)", 2D) = "white" {}
		_Color("颜色(RGBA) ", Color) = (1,1,1,1)
	}

		//--------------------------------【子着色器】--------------------------------  
		SubShader
	{
		Pass
	{
		//关闭光照
		Lighting Off

		//裁剪背面
		Cull back

		//目标Alpha通道混合
		Blend SrcAlpha OneMinusSrcAlpha

		//标记（队列：透明）
		Tags{ Queue = Transparent }

		//纹理与颜色混合 
		SetTexture[_MainTex]
	{
		ConstantColor[_Color]
		Combine Texture * constant
	}
	}
	}
}