using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KoganeUnityLib
{
	/// <summary>
	/// TMP_FontAssetUpdaterSettings の Inspector を拡張するクラス
	/// </summary>
	[CanEditMultipleObjects]
	[CustomEditor( typeof( TMP_FontAssetUpdaterSettings ) )]
	public sealed class TMP_FontAssetUpdaterSettingsEditor : Editor
	{
		//==============================================================================
		// 関数
		//==============================================================================
		/// <summary>
		/// GUI を描画する時に呼び出されます
		/// </summary>
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			// Update ボタンが押された
			if ( GUILayout.Button( "Update" ) )
			{
				OnUpdate();
			}
		}

		/// <summary>
		/// FontAsset を更新します
		/// </summary>
		private void OnUpdate()
		{
			var count = targets.Length;

			// 複数の TMP_FontAssetUpdaterSettings が選択されている場合
			if ( 1 < count )
			{
				OnUpdateMulti();
			}
			// 1 つの TMP_FontAssetUpdaterSettings が選択されている場合
			else if ( 1 == count )
			{
				OnUpdateSingle();
			}
		}

		/// <summary>
		/// 複数の FontAsset を順番に更新します
		/// </summary>
		private void OnUpdateMulti()
		{
			var list = targets
				.Select( c => c as TMP_FontAssetUpdaterSettings )
				.Where( c => c != null )
			;

			if ( !list.Any() ) return;

			var task = new SingleTask();

			foreach ( var n in list )
			{
				task.Add( onEnded =>
				{
					var updater = new TMP_FontAssetUpdater();
					updater.Start( n, onEnded );
				} );
			}

			task.Play();
		}

		/// <summary>
		/// 1 つの FontAsset を更新します
		/// </summary>
		private void OnUpdateSingle()
		{
			var updater		= new TMP_FontAssetUpdater();
			var settings =	 target as TMP_FontAssetUpdaterSettings;

			updater.Start( settings );
		}
	}
}