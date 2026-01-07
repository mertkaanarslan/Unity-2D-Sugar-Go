using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Board : MonoBehaviour
{
    [Header("Board Ayarları")]
    public int width = 8;
    public int height = 8;
    public float padding = 1.2f; // Şekerler arası boşluk

    [Header("Objeler")]
    public GameObject[] candyPrefabs; // Şeker çeşitleri
    public GameObject explosionPrefab; // Patlama efekti (Particle System)

    private Tile firstSelectedTile;
    public GameObject[,] allCandies; // Mantıksal harita

    void Start()
    {
        allCandies = new GameObject[width, height];
        GenerateBoard();
    }

    void GenerateBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Başlangıçta hazır patlamış şeker oluşmasın diye kontrol
                int randomIndex = Random.Range(0, candyPrefabs.Length);
                while (MatchesAt(x, y, candyPrefabs[randomIndex]))
                {
                    randomIndex = Random.Range(0, candyPrefabs.Length);
                }
                SpawnCandy(x, y, candyPrefabs[randomIndex]);
            }
        }
    }

    private void SpawnCandy(int x, int y, GameObject prefab)
    {
        // İlk oluşumda animasyon olmasın diye direkt yerine koyuyoruz
        GameObject newCandy = Instantiate(prefab, new Vector2(x * padding, y * padding), Quaternion.identity);
        newCandy.transform.parent = this.transform;
        newCandy.name = "Candy (" + x + ", " + y + ")";
        
        allCandies[x, y] = newCandy;
        
        Tile tile = newCandy.GetComponent<Tile>();
        tile.Init(x, y, this);
    }

    private bool MatchesAt(int column, int row, GameObject piece)
    {
        if (column > 1) {
            if (allCandies[column - 1, row].GetComponent<SpriteRenderer>().sprite == piece.GetComponent<SpriteRenderer>().sprite &&
                allCandies[column - 2, row].GetComponent<SpriteRenderer>().sprite == piece.GetComponent<SpriteRenderer>().sprite) return true;
        }
        if (row > 1) {
            if (allCandies[column, row - 1].GetComponent<SpriteRenderer>().sprite == piece.GetComponent<SpriteRenderer>().sprite &&
                allCandies[column, row - 2].GetComponent<SpriteRenderer>().sprite == piece.GetComponent<SpriteRenderer>().sprite) return true;
        }
        return false;
    }

    public void SelectTile(Tile clickedTile)
    {
        if (firstSelectedTile == null)
        {
            // İlk seçim
            firstSelectedTile = clickedTile;
            firstSelectedTile.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
        }
        else
        {
            // İkinci seçim
            firstSelectedTile.GetComponent<SpriteRenderer>().color = Color.white;
            
            // Yan yana mı kontrolü
            if (Mathf.Abs(firstSelectedTile.x - clickedTile.x) + Mathf.Abs(firstSelectedTile.y - clickedTile.y) == 1)
            {
                StartCoroutine(SwapTilesCoroutine(firstSelectedTile, clickedTile));
            }
            
            firstSelectedTile = null;
        }
    }

    private void SwapPositions(Tile tile1, Tile tile2)
    {
        // Sadece mantıksal koordinatları değiştiriyoruz.
        // Tile.cs'deki Update fonksiyonu animasyonu yapacak.
        int tempX = tile1.x;
        int tempY = tile1.y;
        
        tile1.x = tile2.x;
        tile1.y = tile2.y;
        
        tile2.x = tempX;
        tile2.y = tempY;

        allCandies[tile1.x, tile1.y] = tile1.gameObject;
        allCandies[tile2.x, tile2.y] = tile2.gameObject;

        tile1.name = "Candy (" + tile1.x + ", " + tile1.y + ")";
        tile2.name = "Candy (" + tile2.x + ", " + tile2.y + ")";
    }

    private IEnumerator SwapTilesCoroutine(Tile tile1, Tile tile2)
    {
        SwapPositions(tile1, tile2);
        
        // Hareket animasyonu için bekle
        yield return new WaitForSeconds(0.4f);

        if (CheckMatches())
        {
            StartCoroutine(DestroyAndRefill());
        }
        else
        {
            SwapPositions(tile1, tile2); // Eşleşme yoksa geri al
        }
    }

    private bool CheckMatches()
    {
        // Sadece kontrol eder, işlem yapmaz
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                GameObject current = allCandies[x, y];
                if(current != null) {
                    if (x < width - 2) {
                        GameObject c2 = allCandies[x + 1, y];
                        GameObject c3 = allCandies[x + 2, y];
                        if (c2 != null && c3 != null) {
                            if (current.GetComponent<SpriteRenderer>().sprite == c2.GetComponent<SpriteRenderer>().sprite &&
                                c2.GetComponent<SpriteRenderer>().sprite == c3.GetComponent<SpriteRenderer>().sprite) return true;
                        }
                    }
                    if (y < height - 2) {
                        GameObject c2 = allCandies[x, y + 1];
                        GameObject c3 = allCandies[x, y + 2];
                        if (c2 != null && c3 != null) {
                            if (current.GetComponent<SpriteRenderer>().sprite == c2.GetComponent<SpriteRenderer>().sprite &&
                                c2.GetComponent<SpriteRenderer>().sprite == c3.GetComponent<SpriteRenderer>().sprite) return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private IEnumerator DestroyAndRefill()
    {
        bool matchesFound = true;
        while (matchesFound)
        {
            yield return new WaitForSeconds(0.1f);
            
            // Yok edilecek şekerleri topladığımız liste
            HashSet<GameObject> matchedCandies = new HashSet<GameObject>();
            
            // Aynı turda, aynı şekere birden fazla efekt çıkmasın diye bu listeyi tutacağız
            List<GameObject> explosionTracker = new List<GameObject>();

            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    GameObject current = allCandies[x, y];
                    if (current != null) {
                        
                        // --- YATAY KONTROL ---
                        if (x < width - 2) {
                            GameObject c2 = allCandies[x + 1, y];
                            GameObject c3 = allCandies[x + 2, y];
                            if (c2 != null && c3 != null) {
                                Sprite s1 = current.GetComponent<SpriteRenderer>().sprite;
                                Sprite s2 = c2.GetComponent<SpriteRenderer>().sprite;
                                Sprite s3 = c3.GetComponent<SpriteRenderer>().sprite;
                                
                                if (s1 == s2 && s2 == s3) { 
                                    matchedCandies.Add(current); 
                                    matchedCandies.Add(c2); 
                                    matchedCandies.Add(c3);
                                    
                                    // YENİ: Sadece ortadaki şekerde (c2) patlama yap
                                    // Ancak o noktada zaten patlama yaptıysak tekrar yapma
                                    if (!explosionTracker.Contains(c2))
                                    {
                                        CreateExplosion(c2);
                                        explosionTracker.Add(c2);
                                    }
                                }
                            }
                        }

                        // --- DİKEY KONTROL ---
                        if (y < height - 2) {
                            GameObject c2 = allCandies[x, y + 1];
                            GameObject c3 = allCandies[x, y + 2];
                            if (c2 != null && c3 != null) {
                                Sprite s1 = current.GetComponent<SpriteRenderer>().sprite;
                                Sprite s2 = c2.GetComponent<SpriteRenderer>().sprite;
                                Sprite s3 = c3.GetComponent<SpriteRenderer>().sprite;
                                
                                if (s1 == s2 && s2 == s3) { 
                                    matchedCandies.Add(current); 
                                    matchedCandies.Add(c2); 
                                    matchedCandies.Add(c3);

                                    // YENİ: Sadece ortadaki şekerde (c2) patlama yap
                                    if (!explosionTracker.Contains(c2))
                                    {
                                        CreateExplosion(c2);
                                        explosionTracker.Add(c2);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (matchedCandies.Count == 0) { matchesFound = false; break; }

            // Şekerleri sessizce yok et (Efekt kodu buradan kaldırıldı)
            foreach (GameObject candy in matchedCandies) {
                if (candy != null) {
                    Tile tile = candy.GetComponent<Tile>();
                    allCandies[tile.x, tile.y] = null;
                    Destroy(candy);
                }
            }
            
            yield return new WaitForSeconds(0.3f);
            ApplyGravity();
            yield return new WaitForSeconds(0.4f);
        }
    }

    // Efekt oluşturma işini yapan küçük, temiz fonksiyon
    private void CreateExplosion(GameObject candy)
    {
        if(explosionPrefab != null)
        {
            GameObject vfx = Instantiate(explosionPrefab, candy.transform.position, Quaternion.identity);
            
            // Efektin rengini şekere göre ayarla
            Tile tile = candy.GetComponent<Tile>();
            ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null && tile != null)
            {
                var mainSettings = ps.main;
                mainSettings.startColor = tile.candyColor; 
            }
            
            // Order in Layer ayarını kodla da garantiye alalım (Şekerlerin önünde görünsün)
            ParticleSystemRenderer psr = vfx.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.sortingOrder = 20; // Yüksek bir sayı veriyoruz
            }

            Destroy(vfx, 1f); 
        }
    }

    private void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            List<GameObject> candiesInColumn = new List<GameObject>();
            for (int y = 0; y < height; y++)
            {
                if (allCandies[x, y] != null) candiesInColumn.Add(allCandies[x, y]);
            }

            for (int y = 0; y < height; y++)
            {
                if (y < candiesInColumn.Count)
                {
                    // Var olanları aşağı kaydır
                    GameObject candy = candiesInColumn[y];
                    allCandies[x, y] = candy;
                    candy.GetComponent<Tile>().x = x;
                    candy.GetComponent<Tile>().y = y;
                    candy.name = "Candy (" + x + ", " + y + ")";
                }
                else
                {
                    // Boş kalan üst kısımlara YENİ şeker üret (Yukarıdan kayarak gelir)
                    int randomIndex = Random.Range(0, candyPrefabs.Length);
                    
                    // Ekranın üstünden başlasın
                    Vector2 spawnPos = new Vector2(x * padding, height * padding + 1); 
                    
                    GameObject newCandy = Instantiate(candyPrefabs[randomIndex], spawnPos, Quaternion.identity);
                    newCandy.transform.parent = this.transform;
                    newCandy.name = "Candy (" + x + ", " + y + ")";
                    allCandies[x, y] = newCandy;
                    
                    Tile tile = newCandy.GetComponent<Tile>();
                    tile.Init(x, y, this);
                }
            }
        }
    }
}