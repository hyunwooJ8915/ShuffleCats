using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [뮤버 데이터 테이블 SO]
/// 구글 시트에서 동기화된 모든 뮤버 데이터를 담는 ScriptableObject.
/// 클래스명과 파일명이 일치해야 빌드에서 SO 바인딩이 정상 동작합니다.
/// </summary>
[CreateAssetMenu(fileName = "MewberTable", menuName = "Data/MewberTable")]
public class MewberTable : ScriptableObject
{
    public List<MewberData> mewbers = new List<MewberData>();
}
