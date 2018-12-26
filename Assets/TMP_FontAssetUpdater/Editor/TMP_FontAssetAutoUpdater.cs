using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KoganeUnityLib
{
	/// <summary>
	/// TMP_FontAssetUpdaterSettings が参照している TextAsset が更新されたら
	/// FontAsset を更新する監視クラス
	/// </summary>
	public sealed class TMP_FontAssetAutoUpdater : AssetPostprocessor
	{
		/// <summary>
		/// いずれかのアセットが変更された時に呼び出されます
		/// </summary>
		private static void OnPostprocessAllAssets
		(
			string[] importedAssets			,
			string[] deletedAssets			,
			string[] movedAssets			,
			string[] movedFromAssetPaths
		)
		{
			// TextAsset に変更があったかどうかを確認
			var textAssetList = importedAssets
				.Where( c => c.EndsWith( ".txt" ) )
				.Select( c => AssetDatabase.LoadAssetAtPath<TextAsset>( c ) )
				.Where( c => c != null )
			;

			// 変更が無い場合は無視
			if ( !textAssetList.Any() ) return;

			// Unity プロジェクトに含まれている TMP_FontAssetUpdaterSettings をすべて取得
			var settingsList = AssetDatabase
				.FindAssets( "t:TMP_FontAssetUpdaterSettings" )
				.Select( c => AssetDatabase.GUIDToAssetPath( c ) )
				.Select( c => AssetDatabase.LoadAssetAtPath<TMP_FontAssetUpdaterSettings>( c ) )
				.Where( c => c != null )
				.Where( c => c.IsAutoUpdate )
				.ToArray()
			;

			// 存在しない場合は無視
			if ( !settingsList.Any() ) return;

			// 変更があった TextAsset が TMP_FontAssetUpdaterSettings で参照されているか確認
			var targets = settingsList
				.Where( c => textAssetList.Contains( c.CustomCharacterList ) )
			;

			// すべて参照されていない場合は無視
			if ( !targets.Any() ) return;

			// TMP_FontAssetUpdaterSettings が参照している TextAsset に変更があった場合は
			// 該当する FontAsset を更新する
			var task = new SingleTask();
			foreach ( var n in targets )
			{
				task.Add( onEnded =>
				{
					var updater = new TMP_FontAssetUpdater();
					updater.Start( n, onEnded );
				} );
			}
			task.Play();
		}
	}
}