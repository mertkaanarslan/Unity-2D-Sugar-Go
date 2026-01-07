using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x;
    public int y;
    public Color candyColor = Color.white; 

    private Board board;
    private float moveSpeed = 15f;

    // Sürükleme (Swipe) için değişkenler
    private Vector2 firstTouchPosition;
    private Vector2 finalTouchPosition;
    private float swipeAngle = 0;
    private float swipeResist = 0.5f; // Sürükleme hassasiyeti (Çok küçük hareketleri yok sayar)

    public void Init(int _x, int _y, Board _board)
    {
        x = _x;
        y = _y;
        board = _board;
    }

    void Update()
    {
        if (board != null)
        {
            // Yumuşak hareket (Lerp)
            Vector2 targetPosition = new Vector2(x * board.padding, y * board.padding);
            if (Vector2.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector2.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = targetPosition;
            }
        }
    }

    // --- SÜRÜKLEME MANTIĞI BURADA BAŞLIYOR ---

    private void OnMouseDown()
    {
        // Tıklama başladığında farenin dünya üzerindeki konumunu al
        firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void OnMouseUp()
    {
        // Tıklama bittiğinde son konumu al
        finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // Başlangıç ve bitiş noktası arasındaki mesafeyi ölç
        if (Mathf.Abs(finalTouchPosition.y - firstTouchPosition.y) > swipeResist || 
            Mathf.Abs(finalTouchPosition.x - firstTouchPosition.x) > swipeResist)
        {
            // Eğer yeterince sürüklediysek açıyı hesapla
            CalculateAngle();
        }
        else
        {
            // Eğer çok az hareket ettirdiysek bunu "Tıklama" olarak kabul et
            // (Hala tıklayarak oynamak isteyenler için)
            if(board != null) board.SelectTile(this);
        }
    }

    void CalculateAngle()
    {
        // İki nokta arasındaki açıyı (radyan cinsinden) hesapla ve dereceye çevir
        swipeAngle = Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y, finalTouchPosition.x - firstTouchPosition.x) * 180 / Mathf.PI;
        
        MovePieces();
    }

    void MovePieces()
    {
        // Açıyı yönlere böl: Sağ, Yukarı, Sol, Aşağı
        if (swipeAngle > -45 && swipeAngle <= 45 && x < board.width - 1)
        {
            // Sağ
            AttemptSwap(1, 0); 
        }
        else if (swipeAngle > 45 && swipeAngle <= 135 && y < board.height - 1)
        {
            // Yukarı
            AttemptSwap(0, 1);
        }
        else if ((swipeAngle > 135 || swipeAngle <= -135) && x > 0)
        {
            // Sol
            AttemptSwap(-1, 0);
        }
        else if (swipeAngle < -45 && swipeAngle >= -135 && y > 0)
        {
            // Aşağı
            AttemptSwap(0, -1);
        }
    }

    void AttemptSwap(int xDir, int yDir)
    {
        // Board scriptine diyoruz ki:
        // 1. Önce BENİ seç (Birinci şeker)
        board.SelectTile(this);
        
        // 2. Sonra YANIMDAKİNİ seç (İkinci şeker) -> Bu otomatik olarak yer değişimi başlatır
        GameObject neighbor = board.allCandies[x + xDir, y + yDir];
        if(neighbor != null)
        {
            board.SelectTile(neighbor.GetComponent<Tile>());
        }
        else
        {
            // Eğer komşu boşsa veya yoksa seçimi iptal et (Hata olmasın)
            board.SelectTile(this); 
        }
    }
}