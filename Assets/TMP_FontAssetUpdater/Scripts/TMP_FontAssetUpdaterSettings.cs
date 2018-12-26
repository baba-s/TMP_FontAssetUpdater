using TMPro;
using TMPro.EditorUtilities;
using UnityEngine;

namespace KoganeUnityLib
{
	/// <summary>
	/// Font Asset Creator の Packing Method の種類
	/// </summary>
	public enum PackingMethods
	{
		Fast	= 0,
		Optimum	= 4,
	};

	/// <summary>
	/// Font Asset Creator の Sampling Point Size の種類
	/// </summary>
	public enum SamplingPointSizes
	{
		AutoSizing	,
		CustomSize	,
	};

	/// <summary>
	/// FontAsset の自動更新の設定を管理する設定クラス
	/// </summary>
	[CreateAssetMenu]
	public sealed class TMP_FontAssetUpdaterSettings : ScriptableObject
	{
		//==============================================================================
		// 変数(SerializeField)
		//==============================================================================
		[SerializeField] private TMP_FontAsset		m_fontAsset				= null;
		[SerializeField] private Font				m_sourceFontFile		= null;
		[SerializeField] private TextAsset			m_customCharacterList	= null;
		[SerializeField] private SamplingPointSizes	m_samplingPointSizeMode	= SamplingPointSizes.AutoSizing;
		[SerializeField] private int				m_samplingPointSize		= 0;
		[SerializeField] private int				m_padding				= 5;
		[SerializeField] private PackingMethods		m_packingMethod			= PackingMethods.Fast;
		[SerializeField] private Vector2Int			m_atlasResolution		= new Vector2Int( 1024, 1024 );
		[SerializeField] private FaceStyles			m_fontStyle				= FaceStyles.Normal;
		[SerializeField] private float				m_fontStyleModifier		= 2;
		[SerializeField] private RenderModes		m_renderMode			= RenderModes.DistanceField16;
		[SerializeField] private bool				m_getKerningPairs		= false;
		[SerializeField] private bool				m_isAutoUpdate			= false;

		//==============================================================================
		// プロパティ
		//==============================================================================
		public TMP_FontAsset		FontAsset				{ get { return m_fontAsset				; } }
		public Font					SourceFontFile			{ get { return m_sourceFontFile			; } }
		public TextAsset			CustomCharacterList		{ get { return m_customCharacterList	; } }
		public SamplingPointSizes	SamplingPointSizeMode	{ get { return m_samplingPointSizeMode	; } }
		public int					SamplingPointSize		{ get { return m_samplingPointSize		; } }
		public int					Padding					{ get { return m_padding				; } }
		public PackingMethods		PackingMethod			{ get { return m_packingMethod			; } }
		public Vector2Int			AtlasResolution			{ get { return m_atlasResolution		; } }
		public FaceStyles			FontStyle				{ get { return m_fontStyle				; } }
		public float				FontStyleModifier		{ get { return m_fontStyleModifier		; } }
		public RenderModes			RenderMode				{ get { return m_renderMode				; } }
		public bool					GetKerningPairs			{ get { return m_getKerningPairs		; } }
		public bool					IsAutoUpdate			{ get { return m_isAutoUpdate			; } }
	}
}