using System.Collections.Generic;
using UnityEngine;

// 맵에 배치하는 책장. 플레이어가 다가가 F를 누르면 이 책장의 책들이 정면뷰로 열린다.
// 책장 오브젝트에 Collider2D(Is Trigger 체크)를 달아 상호작용 범위로 쓴다.
[RequireComponent(typeof(Collider2D))]
public class Bookshelf : MonoBehaviour
{
    [Tooltip("이 책장에 꽂힌 책들 (BookData 에셋들을 드래그)")]
    public List<BookData> books = new List<BookData>();
}
