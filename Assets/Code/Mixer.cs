using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Mixer : MonoBehaviour
{
    // - - -
    // Variables
    // - - -
    public static Mixer Instance;

    [SerializeField] private GameObject _mixVFX;
    [SerializeField] private AudioSource _mixSFX;

    [Tooltip("Each element can be created only once")]
    public bool ExclusiveMixMode = true; 

    [Space]
    [SerializeField] private List<ScriptableElement> _allElementData;
    

    // - - -
    // Internals
    // - - -

    private Card _card1;
    private Card _card2;
    private List<Card> _overQueued;

    void Start()
    {
        Instance = this;
        _overQueued = new List<Card>();

        StartCoroutine(SpawnBasics());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            MixQueuedCards();
        }
    }

    private IEnumerator SpawnBasics()
    {
        float x = -3;
        int dCount = _allElementData.Count;
        for (int i = 0; i < dCount; i++)
        {
            if (_allElementData[i].IsBasic())
            {
                Card newCard = GameBoard.Instance.SpawnCard(x, 7);
                newCard.SetElement(_allElementData[i]);

                GameBoard.Instance.PlaceCard(newCard, x, 0f);
                _allElementData.RemoveAt(i);

                dCount--;
                x += 2f;
                i--;

                yield return new WaitForSeconds(.13f);
            }
        }
    }

    // --- 
    // Mixing
    // ---

    public Card MixCards(Card c1, Card c2)
    {
        ScriptableElement mixResult = Mix(c1, c2);

        if (mixResult == null) return null;

        Card newCard = GameBoard.Instance.SpawnCard(c2.IdleX, c2.IdleY);

        newCard.SetElement(mixResult);

        Vector2 posCurrC1 = c1.transform.localPosition;
        Vector2 posCurrC2 = c2.transform.localPosition;

        int mX = (int)((posCurrC1.x + posCurrC2.x) / 2); 
        int mY = (int)((posCurrC1.y + posCurrC2.y) / 2); 

        GameBoard.Instance.PlaceCard(newCard, mX, mY);

        PlayVFX(mX, mY);
        _mixSFX.Play(); 

        return newCard;
    }

    private Card MixQueuedCards()
    {
        if (_card1 != null && _card2 != null)
        {
            Card newCard = MixCards(_card1, _card2);

            ClearQueue();

            return newCard;
        }

        return null;
    }

    private ScriptableElement Mix(Card c1, Card c2)
    {
        int mixHash = c1.EleHash + c2.EleHash;

        for (int i = 0; i < _allElementData.Count; i++)
        {
            if (_allElementData[i].MixHash() == mixHash)
            {
                var mixResult = _allElementData[i];

                if ( ExclusiveMixMode )
                {
                    _allElementData.RemoveAt(i);
                }

                return mixResult;
            }
        }

        return null;
    }

    private void PlayVFX(int x, int y)
    {
        Instantiate(_mixVFX, new Vector3(x, y, 0), Quaternion.identity, transform);
    }

    // ---
    // Queue
    // ---

    public int QueueForMix(Card card)
    {
        if ( _card1 == null || _card1 == card ) { _card1 = card; return 1; }
        if ( _card2 == null || _card2 == card ) { _card2 = card; return 1; }

        if ( _overQueued.Contains(card)) { return -1; }
        if (!_overQueued.Contains(card)) { _overQueued.Add(card); return -1; }

        return 0;
    }

    public int DeQueue(Card card)
    {
        bool removed = false;

             if (_card1 == card) { _card1 = null; removed = true; }
        else if (_card2 == card) { _card2 = null; removed = true; }

        if (!removed)
        {
            _overQueued.Remove(card);
        }
        else
        {
            if (_overQueued.Count > 0)
            {
                     if (_card1 == null) { _card1 = _overQueued[0]; _card1.SetQueued(1); _overQueued.RemoveAt(0); }
                else if (_card2 == null) { _card2 = _overQueued[0]; _card2.SetQueued(1); _overQueued.RemoveAt(0); }
            }
        }

        return 0;
    }

    private void ClearQueue()
    {
        _card1?.ResetSelection(); _card1 = null;
        _card2?.ResetSelection(); _card2 = null;

        foreach (var card in _overQueued ) card.ResetSelection();

        _overQueued.Clear();
    }
}
