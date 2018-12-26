using System;
using System.Collections;
using System.Collections.Generic;

namespace KoganeUnityLib
{
	/// <summary>
	/// 直列でタスクを管理するクラス
	/// </summary>
	public class SingleTask : IEnumerable
	{
		private readonly List<Action<Action>> mList = new List<Action<Action>>();

		private bool mIsPlaying;

		/// <summary>
		/// タスクを追加します
		/// </summary>
		public void Add( Action<Action> task )
		{
			if ( task == null || mIsPlaying )
			{
				return;
			}

			mList.Add( task );
		}

		/// <summary>
		/// タスクを実行します
		/// </summary>
		public void Play( Action onCompleted = null )
		{
			if ( onCompleted == null )
			{
				onCompleted = delegate { };
			}

			if ( mList.Count <= 0 )
			{
				onCompleted();
				return;
			}

			int count = 0;

			Action task = null;
			task = () =>
			{
				if ( mList.Count <= count )
				{
					onCompleted();
					mIsPlaying = false;
					return;
				}

				Action nextTask = task;

				mList[ count++ ]( ( )=>
				{
					if ( nextTask == null )
					{
						return;
					}
					nextTask();
					nextTask = null;
				} );
			};

			mIsPlaying = true;
			task();
		}

		/// <summary>
		/// コレクションを反復処理する列挙子を返します
		/// </summary>
		public IEnumerator GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}
}