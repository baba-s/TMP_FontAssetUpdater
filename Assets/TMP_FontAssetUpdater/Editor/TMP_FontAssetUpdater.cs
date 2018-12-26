using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace KoganeUnityLib
{
	/// <summary>
	/// FontAsset を更新するクラス
	/// </summary>
	public sealed class TMP_FontAssetUpdater
	{
		//==============================================================================
		// 変数
		//==============================================================================
		private TMP_FontAssetUpdaterSettings	m_settings			;
		private string							m_fontAssetPath		;
		private string							m_characterSequence	;
		private Font							m_font				;
		private FT_FaceInfo						m_fontFaceInfo		;
		private FT_GlyphInfo[]					m_fontGlyphInfo		;
		private byte[]							m_textureBuffer		;
		private Texture2D						m_fontAtlas			;
		private int[]							m_kerningSet		;
		private int								m_characterCount	;
		private string[]						m_assets			;

		//==============================================================================
		// プロパティ
		//==============================================================================
		private SamplingPointSizes	SamplingPointSizeMode	{ get { return m_settings.SamplingPointSizeMode; } }
		private int					SamplingPointSize		{ get { return SamplingPointSizeMode == SamplingPointSizes.AutoSizing ? 72 : m_settings.SamplingPointSize; } }
		private int					Padding					{ get { return m_settings.Padding; } }
		private PackingMethods		PackingMethod			{ get { return m_settings.PackingMethod; } }
		private int					AtlasWidth				{ get { return m_settings.AtlasResolution.x; } }
		private int					AtlasHeight				{ get { return m_settings.AtlasResolution.y; } }
		private FaceStyles			FontStyle				{ get { return m_settings.FontStyle; } }
		private float				FontStyleModifier		{ get { return m_settings.FontStyleModifier; } }
		private RenderModes			RenderMode				{ get { return m_settings.RenderMode; } }
		private bool				IncludeKerningPairs		{ get { return m_settings.GetKerningPairs; } }

		//==============================================================================
		// 関数
		//==============================================================================
		public void Start( TMP_FontAssetUpdaterSettings settings, Action onComplete = null )
		{
			ShaderUtilities.GetShaderPropertyIDs();

			m_settings		= settings;
			m_font			= settings.SourceFontFile;
			m_fontAssetPath	= AssetDatabase.GetAssetPath( settings.FontAsset );

			var fontPath	= AssetDatabase.GetAssetPath( m_font );
			var fontName	= Path.GetFileNameWithoutExtension( fontPath );
			var filter		= "t:TMP_FontAsset " + fontName + " SDF";

			m_assets = AssetDatabase
				.FindAssets( filter, new [] { "Assets" } )
				.Select( c => AssetDatabase.GUIDToAssetPath( c ) )
				.ToArray()
			;

			m_characterSequence = m_settings.CustomCharacterList.text;

			if ( m_font == null ) return;

			GameObject.DestroyImmediate( m_fontAtlas );
			m_fontAtlas = null;

			var initResult = TMPro_FontPlugin.Initialize_FontEngine();
			if ( initResult != 0 && initResult != 0xF0 )
			{
				Debug.Log( "Error Code: " + initResult + "  occurred while Initializing the FreeType Library." );
				return;
			}

			var loadResult = TMPro_FontPlugin.Load_TrueType_Font( fontPath );
			if ( loadResult != 0 && loadResult != 0xF1 )
			{
				Debug.Log( "Error Code: " + loadResult + "  occurred while Loading the [" + m_font.name + "] font file. This typically results from the use of an incompatible or corrupted font file." );
				return;
			}

			var sizeResult = TMPro_FontPlugin.FT_Size_Font( SamplingPointSize );
			if ( sizeResult != 0 )
			{
				Debug.Log( "Error Code: " + sizeResult + "  occurred while Sizing the font." );
				return;
			}

			int[] characterSet;
			var charList = new List<int>();

			for ( int i = 0; i < m_characterSequence.Length; i++ )
			{
				if ( charList.FindIndex( item => item == m_characterSequence[ i ] ) == -1 )
				{
					charList.Add( m_characterSequence[ i ] );
				}
				else
				{
					//Debug.Log("Character [" + characterSequence[i] + "] is a duplicate.");
				}
			}

			characterSet = charList.ToArray();

			m_characterCount	= characterSet.Length;
			m_textureBuffer		= new byte[ AtlasWidth * AtlasHeight ];
			m_fontFaceInfo		= new FT_FaceInfo();
			m_fontGlyphInfo		= new FT_GlyphInfo[ m_characterCount ];

			int padding = Padding;

			var autoSizing = SamplingPointSizeMode == SamplingPointSizes.AutoSizing;
			var strokeSize = FontStyleModifier;

			if ( RenderMode == RenderModes.DistanceField16 ) strokeSize = FontStyleModifier * 16;
			if ( RenderMode == RenderModes.DistanceField32 ) strokeSize = FontStyleModifier * 32;

			EditorApplication.update += OnUpdate;

			ThreadPool.QueueUserWorkItem( _ =>
			{
				TMPro_FontPlugin.Render_Characters
				(
					buffer				: m_textureBuffer,
					buffer_width		: AtlasWidth,
					buffer_height		: AtlasHeight,
					character_padding	: padding,
					asc_set				: characterSet,
					char_count			: m_characterCount,
					style				: FontStyle,
					style_mod			: strokeSize,
					autoSize			: autoSizing,
					renderMode			: RenderMode,
					method				: ( int )PackingMethod,
					fontData			: ref m_fontFaceInfo,
					Output				: m_fontGlyphInfo
				);

				EditorApplication.delayCall += () =>
				{
					OnDone();

					if ( onComplete != null )
					{
						onComplete();
					}
				};
			} );
		}

		private void OnUpdate()
		{
			var info = m_fontAssetPath;
			var progress = TMPro_FontPlugin.Check_RenderProgress();
			EditorUtility.DisplayProgressBar( "TMP_FontAssetUpdater", info, progress );
		}

		private void OnDone()
		{
			EditorUtility.ClearProgressBar();

			EditorApplication.update -= OnUpdate;

			UpdateRenderFeedbackWindow();
			CreateFontTexture();

			foreach ( var asset in m_assets )
			{
				Save_SDF_FontAsset( asset );
			}

			m_fontAtlas = null;

			TMPro_FontPlugin.Destroy_FontEngine();

			if ( m_fontAtlas != null && EditorUtility.IsPersistent( m_fontAtlas ) == false )
			{
				GameObject.DestroyImmediate( m_fontAtlas );
			}

			Resources.UnloadUnusedAssets();
		}

		/// <summary>
		/// Function to update the feedback window showing the results of the latest generation.
		/// </summary>
		private void UpdateRenderFeedbackWindow()
		{
			string colorTag = m_fontFaceInfo.characterCount == m_characterCount ? "<color=#ff0000>" : "<color=#ff0000>";
			string colorTag2 = "<color=#ff0000>";

			var missingGlyphReport = "Font: " + "<b>" + colorTag2 + m_fontFaceInfo.name + "</color></b>";

			var k_OutputSizeLabel = "Pt. Size: ";
			if ( missingGlyphReport.Length > 60 )
				missingGlyphReport += "\n" + k_OutputSizeLabel + "<b>" + colorTag2 + m_fontFaceInfo.pointSize + "</color></b>";
			else
				missingGlyphReport += "  " + k_OutputSizeLabel + "<b>" + colorTag2 + m_fontFaceInfo.pointSize + "</color></b>";

			missingGlyphReport += "\n" + "Characters packed: " + "<b>" + colorTag + m_fontFaceInfo.characterCount + "/" + m_characterCount + "</color></b>";

			// Report missing requested glyph
			missingGlyphReport += "\n\n<color=#ff0000><b>Missing Characters</b></color>";
			missingGlyphReport += "\n----------------------------------------";

			var m_OutputFeedback = missingGlyphReport;

			for ( int i = 0; i < m_characterCount; i++ )
			{
				if ( m_fontGlyphInfo[ i ].x == -1 )
				{
					missingGlyphReport += "\nID: <color=#ff0000>" + m_fontGlyphInfo[ i ].id + "\t</color>Hex: <color=#ff0000>" + m_fontGlyphInfo[ i ].id.ToString( "X" ) + "\t</color>Char [<color=#ff0000>" + ( char )m_fontGlyphInfo[ i ].id + "</color>]";

					if ( missingGlyphReport.Length < 16300 )
						m_OutputFeedback = missingGlyphReport;
				}
			}

			if ( missingGlyphReport.Length > 16300 )
				m_OutputFeedback += "\n\n<color=#ff0000>Report truncated.</color>\n<color=#c0ffff>See</color> \"TextMesh Pro\\Glyph Report.txt\"";

			Debug.Log( m_OutputFeedback );
		}

		private void CreateFontTexture()
		{
			m_fontAtlas = new Texture2D( AtlasWidth, AtlasHeight, TextureFormat.Alpha8, false, true );

			Color32[] colors = new Color32[ AtlasWidth * AtlasHeight ];

			for ( int i = 0; i < ( AtlasWidth * AtlasHeight ); i++ )
			{
				byte c = m_textureBuffer[ i ];
				colors[ i ] = new Color32( c, c, c, c );
			}
			// Clear allocation of
			m_textureBuffer = null;

			if ( RenderMode == RenderModes.Raster || RenderMode == RenderModes.RasterHinted )
				m_fontAtlas.filterMode = FilterMode.Point;

			m_fontAtlas.SetPixels32( colors, 0 );
			m_fontAtlas.Apply( false, true );
		}

		private void Save_SDF_FontAsset( string filePath )
		{
			string relativeAssetPath = filePath;
			string tex_DirName = Path.GetDirectoryName( relativeAssetPath );
			string tex_FileName = Path.GetFileNameWithoutExtension( relativeAssetPath );
			string tex_Path_NoExt = tex_DirName + "/" + tex_FileName;

			// Check if TextMeshPro font asset already exists. If not, create a new one. Otherwise update the existing one.
			var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>( tex_Path_NoExt + ".asset" );
			if ( fontAsset == null )
			{
				//Debug.Log("Creating TextMeshPro font asset!");
				fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>(); // Create new TextMeshPro Font Asset.
				AssetDatabase.CreateAsset( fontAsset, tex_Path_NoExt + ".asset" );

				//Set Font Asset Type
				fontAsset.fontAssetType = TMP_FontAsset.FontAssetTypes.SDF;

				// If using the C# SDF creation mode, we need the scale down factor.
				int scaleDownFactor = 1; // ((RasterModes)m_RenderMode & RasterModes.Raster_Mode_SDF) == RasterModes.Raster_Mode_SDF || m_RenderMode == RenderModes.DistanceFieldAA ? 1 : font_scaledownFactor;

				// Add FaceInfo to Font Asset
				FaceInfo face = GetFaceInfo( m_fontFaceInfo, scaleDownFactor );
				fontAsset.AddFaceInfo( face );

				// Add GlyphInfo[] to Font Asset
				TMP_Glyph[] glyphs = GetGlyphInfo( m_fontGlyphInfo, scaleDownFactor );
				fontAsset.AddGlyphInfo( glyphs );

				// Get and Add Kerning Pairs to Font Asset
				if ( IncludeKerningPairs )
				{
					string fontFilePath = AssetDatabase.GetAssetPath( m_font );
					KerningTable kerningTable = GetKerningTable( fontFilePath, ( int )face.PointSize );
					fontAsset.AddKerningInfo( kerningTable );
				}

				// Add Font Atlas as Sub-Asset
				fontAsset.atlas = m_fontAtlas;
				if ( !m_fontAtlas.name.EndsWith( " Atlas" ) )
				{
					m_fontAtlas.name = tex_FileName + " Atlas";
					AssetDatabase.AddObjectToAsset( m_fontAtlas, fontAsset );
				}

				// Create new Material and Add it as Sub-Asset
				Shader default_Shader = Shader.Find( "TextMeshPro/Distance Field" ); //m_shaderSelection;
				Material tmp_material = new Material( default_Shader );

				tmp_material.name = tex_FileName + " Material";
				tmp_material.SetTexture( ShaderUtilities.ID_MainTex, m_fontAtlas );
				tmp_material.SetFloat( ShaderUtilities.ID_TextureWidth, m_fontAtlas.width );
				tmp_material.SetFloat( ShaderUtilities.ID_TextureHeight, m_fontAtlas.height );

				int spread = Padding + 1;
				tmp_material.SetFloat( ShaderUtilities.ID_GradientScale, spread ); // Spread = Padding for Brute Force SDF.

				tmp_material.SetFloat( ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle );
				tmp_material.SetFloat( ShaderUtilities.ID_WeightBold, fontAsset.boldStyle );

				fontAsset.material = tmp_material;

				AssetDatabase.AddObjectToAsset( tmp_material, fontAsset );

			}
			else
			{
				// Find all Materials referencing this font atlas.
				Material[] material_references = FindMaterialReferences( fontAsset );

				if ( fontAsset.atlas )
				{
					// Destroy Assets that will be replaced.
					GameObject.DestroyImmediate( fontAsset.atlas, true );
				}

				//Set Font Asset Type
				fontAsset.fontAssetType = TMP_FontAsset.FontAssetTypes.SDF;

				int scaleDownFactor = 1; // ((RasterModes)m_RenderMode & RasterModes.Raster_Mode_SDF) == RasterModes.Raster_Mode_SDF || m_RenderMode == RenderModes.DistanceFieldAA ? 1 : font_scaledownFactor;
										 // Add FaceInfo to Font Asset
				FaceInfo face = GetFaceInfo( m_fontFaceInfo, scaleDownFactor );
				fontAsset.AddFaceInfo( face );

				// Add GlyphInfo[] to Font Asset
				TMP_Glyph[] glyphs = GetGlyphInfo( m_fontGlyphInfo, scaleDownFactor );
				fontAsset.AddGlyphInfo( glyphs );

				// Get and Add Kerning Pairs to Font Asset
				if ( IncludeKerningPairs )
				{
					string fontFilePath = AssetDatabase.GetAssetPath( m_font );
					KerningTable kerningTable = GetKerningTable( fontFilePath, ( int )face.PointSize );
					fontAsset.AddKerningInfo( kerningTable );
				}

				// Add Font Atlas as Sub-Asset
				fontAsset.atlas = m_fontAtlas;
				if ( !m_fontAtlas.name.EndsWith( " Atlas" ) )
				{
					m_fontAtlas.name = tex_FileName + " Atlas";
					AssetDatabase.AddObjectToAsset( m_fontAtlas, fontAsset );
				}

				// Special handling due to a bug in earlier versions of Unity.
				m_fontAtlas.hideFlags = HideFlags.None;
				fontAsset.material.hideFlags = HideFlags.None;

				// Assign new font atlas texture to the existing material.
				fontAsset.material.SetTexture( ShaderUtilities.ID_MainTex, fontAsset.atlas );

				// Update the Texture reference on the Material
				for ( int i = 0; i < material_references.Length; i++ )
				{
					material_references[ i ].SetTexture( ShaderUtilities.ID_MainTex, m_fontAtlas );
					material_references[ i ].SetFloat( ShaderUtilities.ID_TextureWidth, m_fontAtlas.width );
					material_references[ i ].SetFloat( ShaderUtilities.ID_TextureHeight, m_fontAtlas.height );

					int spread = Padding + 1;
					material_references[ i ].SetFloat( ShaderUtilities.ID_GradientScale, spread ); // Spread = Padding for Brute Force SDF.

					material_references[ i ].SetFloat( ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle );
					material_references[ i ].SetFloat( ShaderUtilities.ID_WeightBold, fontAsset.boldStyle );
				}
			}

			fontAsset.ReadFontDefinition();

			AssetDatabase.SaveAssets();

			AssetDatabase.ImportAsset( AssetDatabase.GetAssetPath( fontAsset ) );  // Re-import font asset to get the new updated version.

			AssetDatabase.Refresh();

			// NEED TO GENERATE AN EVENT TO FORCE A REDRAW OF ANY TEXTMESHPRO INSTANCES THAT MIGHT BE USING THIS FONT ASSET
			TMPro_EventManager.ON_FONT_PROPERTY_CHANGED( true, fontAsset );
		}

		// Convert from FT_FaceInfo to FaceInfo
		private static FaceInfo GetFaceInfo( FT_FaceInfo ftFace, int scaleFactor )
		{
			var face = new FaceInfo();

			face.Name = ftFace.name;
			face.PointSize = ( float )ftFace.pointSize / scaleFactor;
			face.Padding = ( float )ftFace.padding / scaleFactor;
			face.LineHeight = ftFace.lineHeight / scaleFactor;
			face.CapHeight = 0;
			face.Baseline = 0;
			face.Ascender = ftFace.ascender / scaleFactor;
			face.Descender = ftFace.descender / scaleFactor;
			face.CenterLine = ftFace.centerLine / scaleFactor;
			face.Underline = ftFace.underline / scaleFactor;
			face.UnderlineThickness = ftFace.underlineThickness == 0 ? 5 : ftFace.underlineThickness / scaleFactor; // Set Thickness to 5 if TTF value is Zero.
			face.strikethrough = ( face.Ascender + face.Descender ) / 2.75f;
			face.strikethroughThickness = face.UnderlineThickness;
			face.SuperscriptOffset = face.Ascender;
			face.SubscriptOffset = face.Underline;
			face.SubSize = 0.5f;
			face.AtlasWidth = ftFace.atlasWidth / scaleFactor;
			face.AtlasHeight = ftFace.atlasHeight / scaleFactor;

			return face;
		}

		// Convert from FT_GlyphInfo[] to GlyphInfo[]
		private TMP_Glyph[] GetGlyphInfo( FT_GlyphInfo[] ftGlyphs, int scaleFactor )
		{
			var glyphs = new List<TMP_Glyph>();
			var kerningSet = new List<int>();

			for ( int i = 0; i < ftGlyphs.Length; i++ )
			{
				var g = new TMP_Glyph();

				g.id = ftGlyphs[ i ].id;
				g.x = ftGlyphs[ i ].x / scaleFactor;
				g.y = ftGlyphs[ i ].y / scaleFactor;
				g.width = ftGlyphs[ i ].width / scaleFactor;
				g.height = ftGlyphs[ i ].height / scaleFactor;
				g.xOffset = ftGlyphs[ i ].xOffset / scaleFactor;
				g.yOffset = ftGlyphs[ i ].yOffset / scaleFactor;
				g.xAdvance = ftGlyphs[ i ].xAdvance / scaleFactor;

				// Filter out characters with missing glyphs.
				if ( g.x == -1 ) continue;

				glyphs.Add( g );
				kerningSet.Add( g.id );
			}

			m_kerningSet = kerningSet.ToArray();

			return glyphs.ToArray();
		}

		// Get Kerning Pairs
		private KerningTable GetKerningTable( string fontFilePath, int pointSize )
		{
			var kerningInfo = new KerningTable();
			kerningInfo.kerningPairs = new List<KerningPair>();

			// Temporary Array to hold the kerning pairs from the Native Plug-in.
			var kerningPairs = new FT_KerningPair[ 7500 ];

			int kpCount = TMPro_FontPlugin.FT_GetKerningPairs( fontFilePath, m_kerningSet, m_kerningSet.Length, kerningPairs );

			for ( int i = 0; i < kpCount; i++ )
			{
				// Proceed to add each kerning pairs.
				var kp = new KerningPair( ( uint )kerningPairs[ i ].ascII_Left, ( uint )kerningPairs[ i ].ascII_Right, kerningPairs[ i ].xAdvanceOffset * pointSize );

				// Filter kerning pairs to avoid duplicates
				int index = kerningInfo.kerningPairs.FindIndex( item => item.firstGlyph == kp.firstGlyph && item.secondGlyph == kp.secondGlyph );

				if ( index == -1 )
					kerningInfo.kerningPairs.Add( kp );
				else
					if ( !TMP_Settings.warningsDisabled ) Debug.LogWarning( "Kerning Key for [" + kp.firstGlyph + "] and [" + kp.secondGlyph + "] is a duplicate." );

			}

			return kerningInfo;
		}

		private static Material[] FindMaterialReferences( TMP_FontAsset fontAsset )
		{
			var materialList = new List<Material>();
			var material1 = fontAsset.material;
			materialList.Add( material1 );
			var str1 = "t:Material ";
			var str2 = fontAsset.name;
			var str3 = "_";

			foreach ( var asset in AssetDatabase.FindAssets( str1 + str2 + str3 ) )
			{
				var material2 = AssetDatabase.LoadAssetAtPath<Material>( AssetDatabase.GUIDToAssetPath( asset ) );

				if ( material2.HasProperty( ShaderUtilities.ID_MainTex ) &&
					material2.GetTexture( ShaderUtilities.ID_MainTex ) != null &&
					( material1.GetTexture( ShaderUtilities.ID_MainTex ) != null &&
					 material2.GetTexture( ShaderUtilities.ID_MainTex ).GetInstanceID() ==
					 material1.GetTexture( ShaderUtilities.ID_MainTex ).GetInstanceID() ) && !materialList.Contains( material2 ) )
					materialList.Add( material2 );
			}
			return materialList.ToArray();
		}
	}
}