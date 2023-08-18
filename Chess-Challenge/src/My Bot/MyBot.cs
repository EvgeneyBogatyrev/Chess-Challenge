using ChessChallenge.API;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    DepthSearcher depthSearcher = new DepthSearcher();

    const int MAXDEPTH = 4;
    const float MATERIAL_COEF = 1f;
    const float POSITIONAL_COEF = 0.5f;
    const float KING_COEF = 5f;


    public static class PieceTables
    {

        /* 
            How to evaluate:
            for (bitmap, coef) in zip(bitmaps, coefs):
                score += coef * CountBits(bitmap & piece_bitmap)
        */

        public static ulong[] pawnStartBitmaps = {
            0b0000000000000000000000000000000000000000000000000001100000000000,
            0b0000000000000000000000000000000000000000001001000000000000000000,
            0b0000000000000000000000000000000000000000010000100000000000000000,
            0b0000000000000000000000001100001100000000100000011000000100000000,
            0b0000000000000000110000110010010000000000000000000110011000000000,
            0b0000000000000000001001000000000000011000000000000000000000000000,
            0b0000000000000000000000000001100000000000000000000000000000000000,
            0b0000000000000000000110000000000000000000000000000000000000000000,
            0b0000000011111111000000000000000000000000000000000000000000000000
        };

        public static int[] pawnStartCoefs = { -20, -10, -5, 5, 10, 20, 25, 30, 50 };


        public static ulong[] pawnEndBitmaps = {
            0b0000000000000000000000000000000000000000111111111111111100000000,
            0b0000000000000000000000000000000011111111000000000000000000000000,
            0b0000000000000000000000001111111100000000000000000000000000000000,
            0b0000000000000000111111110000000000000000000000000000000000000000,
            0b0000000011111111000000000000000000000000000000000000000000000000,
        };

        public static int[] pawnEndCoef = { 10, 20, 30, 50, 80 };


        public static ulong[] rookBitmaps =  {
            0b0000000000000000100000011000000110000001100000011000000100000000,
            0b0000000010000001000000000000000000000000000000000000000000011000,
            0b0000000001111110000000000000000000000000000000000000000000000000,
        };

        public static int[] rookCoef = { -5, 5, 10 };

        public static ulong[] knightBitmaps = {
            0b1000000100000000000000000000000000000000000000000000000010000001,
            0b0100001010000001000000000000000000000000000000001000000101000010,
            0b0011110000000000100000011000000110000001100000010000000000111100,
            0b0000000001000010000000000000000000000000000000000100001000000000,
            0b0000000000000000000000000100001000000000010000100001100000000000,
            0b0000000000000000001001000000000000000000001001000000000000000000,
            0b0000000000000000000110000010010000100100000110000000000000000000,
            0b0000000000000000000000000001100000011000000000000000000000000000,
         };

        public static int[] knightCoef = { -50, -40, -30, -20, 5, 10, 15, 20 };

        public static ulong[] bishopBitmaps =  {
            0b1000000100000000000000000000000000000000000000000000000010000001,
            0b0111111010000001100000011000000110000001100000011000000101111110,
            0b0000000000000000001001000110011000000000000000000100001000000000,
            0b0000000000000000000110000001100000111100011111100000000000000000,
        };

        public static int[] bishopCoef = { -20, -10, 5, 10 };

        public static ulong[] queenBitmaps =  {
            0b1000000100000000000000000000000000000000000000000000000010000001,
            0b0110011010000001100000010000000000000000100000011000000101100110,
            0b0001100000000000000000001000000110000000000000000000000000011000,
            0b0000000000000000001111000011110000111100001111100000010000000000,
        };

        public static int[] queenCoef = { -20, -10, -5, 5 };

        public static ulong[] kingStartBitmaps = {
            0b1000000100000000000000000000000000000000000000000000000000000000,
            0b0111111000000000000000000000000000000000000000000000000000000000,
            0b0000000011111111000110000000000000000000000000000000000000000000,
            0b0000000000000000011001100001100000000000000000000000000000000000,
            0b0000000000000000100000010110011000011000000000000000000000000000,
            0b0000000000000000000000001000000101100110000000000000000000000000,
            0b0000000000000000000000000000000010000001011111100000000000000000,
            0b0000000000000000000000000000000000000000100000010000000000000000,
            0b0000000000000000000000000000000000000000000000000011110000000000,
            0b0000000000000000000000000000000000000000000000000000000000100100,
            0b0000000000000000000000000000000000000000000000001100001110000001,
            0b0000000000000000000000000000000000000000000000000000000001000010,
        };
        public static int[] kingStartCoef = { -80, -70, -60, -50, -40, -30, -20, -10, -5, 10, 20, 30 };

        public static ulong[] kingEndBitmaps = {
            0b0000000000000000000000000000000000000000000000000000000010000001,
            0b0000000000000000000000000000000000000000000000001000000101111110,
            0b0000000000000000000000000000000000000000100000010100001000000000,
            0b1000000100000000000000000000000010000001010000100000000000000000,
            0b0000000000000000000000001000000101000010000000000000000000000000,
            0b0111111000000000100000010100001000000000000000000000000000000000,
            0b0000000010000001010000100000000000000000000000000000000000000000,
            0b0000000000111100000000000000000000000000000000000000000000000000,
            0b0000000000000000001001000000000000000000001001000000000000000000,
            0b0000000000000000000000000000000000000000000110000000000000000000,
            0b0000000000000000000110000000000000100100000000000000000000000000,
            0b0000000000000000000000000010010000000000000000000000000000000000,
            0b0000000000000000000000000000000000011000000000000000000000000000,
            0b0000000000000000000000000001100000000000000000000000000000000000,
        };

        public static int[] kingEndCoef = { -50, -30, -25, -20, -15, -10, -5, 5, 20, 25, 30, 35, 40, 45 };
    }

    public static class Evaluator
    {
        static ulong lastBoardFen = 0;
        static float endgameProbability = 0f;

        public static float GetEndgameValue(Board board)
        {
            if (board.ZobristKey == lastBoardFen)
            {
                // No need to calculate endgame probability again
                return endgameProbability;
            }

            float sumWhite = 0f;
            float sumBlack = 0f;
            foreach (PieceType piece in Enum.GetValues(typeof(PieceType)))
            {
                if (piece == PieceType.Pawn)
                {
                    continue;
                }
                sumWhite += Utils.GetPieceValue(piece, 0f) * Utils.CountBits(board.GetPieceBitboard(piece, true));
                sumBlack += Utils.GetPieceValue(piece, 0f) * Utils.CountBits(board.GetPieceBitboard(piece, false));
            }

            endgameProbability = MathF.Max(0f, 1f - MathF.Min(sumBlack, sumWhite) / 1500f); // Endgame starts when material sum is less than 15
            lastBoardFen = board.ZobristKey; // Save last board
            return endgameProbability;
        }
        public static float CountPiecesValueBalance(Board board)
        {
            float sum = 0f;
            foreach (PieceType piece in Enum.GetValues(typeof(PieceType)))
            {
                sum += Utils.GetPieceValue(piece, GetEndgameValue(board)) * (Utils.CountBits(board.GetPieceBitboard(piece, true)) - Utils.CountBits(board.GetPieceBitboard(piece, false)));
            }
            return sum;
        }
        public static float EvaluateKingsPositions(Board board)
        {
            // To keep enemy king far from center
            Square whiteKingSquare = board.GetKingSquare(board.IsWhiteToMove);
            Square blackKingSquare = board.GetKingSquare(!board.IsWhiteToMove);

            int blackH = Math.Min(3 - blackKingSquare.Rank, blackKingSquare.Rank - 4);
            int blackW = Math.Min(3 - blackKingSquare.File, blackKingSquare.File - 4);

            int whiteH = Math.Min(3 - whiteKingSquare.Rank, whiteKingSquare.Rank - 4);
            int whiteW = Math.Min(3 - whiteKingSquare.File, whiteKingSquare.File - 4);

            return (-whiteH - whiteW + blackH + blackW) * GetEndgameValue(board);
        }

        public static float GetPositionalValue(Board board)
        {
            float value = 0f;
            float valueEndgame = 0f;
            foreach (PieceType piece in Enum.GetValues(typeof(PieceType)))
            {
                ulong[] maps;
                ulong[] mapsEndgame = null;

                int[] coefs;
                int[] coefsEndgame = null;

                switch (piece)
                {
                    case PieceType.Pawn:
                        maps = PieceTables.pawnStartBitmaps;
                        mapsEndgame = PieceTables.pawnEndBitmaps;
                        coefs = PieceTables.pawnStartCoefs;
                        coefsEndgame = PieceTables.pawnEndCoef;
                        break;

                    case PieceType.Knight:
                        maps = PieceTables.knightBitmaps;
                        coefs = PieceTables.knightCoef;
                        break;

                    case PieceType.Bishop:
                        maps = PieceTables.bishopBitmaps;
                        coefs = PieceTables.bishopCoef;
                        break;

                    case PieceType.Rook:
                        maps = PieceTables.rookBitmaps;
                        coefs = PieceTables.rookCoef;
                        break;

                    case PieceType.Queen:
                        maps = PieceTables.queenBitmaps;
                        coefs = PieceTables.queenCoef;
                        break;

                    case PieceType.King:
                        maps = PieceTables.kingStartBitmaps;
                        mapsEndgame = PieceTables.kingEndBitmaps;
                        coefs = PieceTables.kingStartCoef;
                        coefsEndgame = PieceTables.kingEndCoef;
                        break;

                    default:
                        maps = PieceTables.pawnStartBitmaps;
                        coefs = PieceTables.pawnStartCoefs;
                        break;
                }

                for (int i = 0; i < coefs.Count(); ++i)
                {
                    int numberOfPiecesOnThesePositions = Utils.CountBits(maps[i] & board.GetPieceBitboard(piece, true)) - Utils.CountBits(Utils.ReverseBits(maps[i]) & board.GetPieceBitboard(piece, false));
                    value += numberOfPiecesOnThesePositions * coefs[i];
                }

                if (coefsEndgame != null)
                {
                    for (int i = 0; i < coefsEndgame.Count(); ++i)
                    {
                        int numberOfPiecesOnThesePositions = Utils.CountBits(mapsEndgame[i] & board.GetPieceBitboard(piece, true)) - Utils.CountBits(Utils.ReverseBits(mapsEndgame[i]) & board.GetPieceBitboard(piece, false));
                        valueEndgame += numberOfPiecesOnThesePositions * coefsEndgame[i];
                    }
                }
            }
            return value * (1 - Evaluator.endgameProbability) + valueEndgame * (Evaluator.endgameProbability);
        }
    }



    public static class Utils
    {
        public static float GetPieceValue(PieceType piece, float endgame=0f)
        {
            int[] pieceMiddlegameValues = { 0, 100, 300, 300, 500, 900, 0 };
            return pieceMiddlegameValues[(int)piece];
            //int[] pieceMiddlegameValues = {0, 84, 337, 365, 477, 1025, 0 };
            //int[] pieceEndgameValues = {0, 94, 281, 297, 512, 936, 0 };
            //return (1 - endgame) * pieceMiddlegameValues[(int)piece] + endgame * pieceEndgameValues[(int)piece];
        }
        public static int CountBits(ulong value)
        {
            int count = 0;
            while (value != 0)
            {
                count++;
                value &= value - 1;
            }
            return count;
        }

        public static ulong ReverseBits(ulong value)
        {
            ulong newRes = 0;
            for (int i = 0; i < 8; ++i)
            {
                newRes <<= 8;
                newRes |= value & 0x00000000000000FF;
                value >>= 8;
            }
            return newRes;
        }

        // Maybe it's better to use built-in quicksort
        public static void Quicksort(System.Span<Move> values, int[] scores, int low, int high)
        {
            if (low < high)
            {
                int pivotIndex = Partition(values, scores, low, high);
                Quicksort(values, scores, low, pivotIndex - 1);
                Quicksort(values, scores, pivotIndex + 1, high);
            }
        }

        static int Partition(System.Span<Move> values, int[] scores, int low, int high)
        {
            int pivotScore = scores[high];
            int i = low - 1;

            for (int j = low; j <= high - 1; j++)
            {
                if (scores[j] > pivotScore)
                {
                    i++;
                    (values[i], values[j]) = (values[j], values[i]);
                    (scores[i], scores[j]) = (scores[j], scores[i]);
                }
            }
            (values[i + 1], values[high]) = (values[high], values[i + 1]);
            (scores[i + 1], scores[high]) = (scores[high], scores[i + 1]);

            return i + 1;
        }
    }

    public static void OrderMoves(Move[] moves, Board board)
    {
        int[] moveScores = new int[moves.Count()];
        int i = 0;
        float endgame = Evaluator.GetEndgameValue(board);
        foreach (Move move in moves)
        {
            float moveScore = 0f;
            PieceType movedPieceType = move.MovePieceType;
            PieceType capturePieceType = move.CapturePieceType;

            if (capturePieceType != PieceType.None)
            {
                moveScore += 10f * Utils.GetPieceValue(capturePieceType, endgame) - Utils.GetPieceValue(movedPieceType, endgame);
            }

            if (move.IsPromotion)
            {
                moveScore += Utils.GetPieceValue(move.PromotionPieceType, endgame);
            }

            moveScores[i] = (int) moveScore;
            i += 1;
        }

        Utils.Quicksort(moves, moveScores, 0, i - 1);
    }

    public static float EvaluatePosition(Board board, bool log=false)
    {
        float mul = board.IsWhiteToMove ? 1f : -1f;

        float materialValue = MATERIAL_COEF * Evaluator.CountPiecesValueBalance(board);
        float positionalValue = POSITIONAL_COEF * Evaluator.GetPositionalValue(board);
        float kingValue = KING_COEF * Evaluator.EvaluateKingsPositions(board);

        if (log)
        {
            Console.WriteLine("Positional: " + positionalValue.ToString());
            Console.WriteLine("Material: " + materialValue.ToString());
            Console.WriteLine("King: " + kingValue.ToString());
        }

        return mul * (materialValue + positionalValue + kingValue);
    }
    public class DepthSearcher
    {
        private Move bestMove;
        private float INF = 10e9f;

        
        public Move GetMove(Board board)
        {
            return bestMove == Move.NullMove ? board.GetLegalMoves()[0] : bestMove; // To avoid illegal move
        }
        public float DepthSearch(Board board, int depth, int maxDepth, float alpha, float beta, Timer timer)
        {
            if (depth == 0)
            {
                return SearchAllCaptures(board, alpha, beta, timer);
            }

            if (depth == maxDepth)
            {
                bestMove = Move.NullMove;
            }

            Move[] moves = board.GetLegalMoves();

            if (moves.Length == 0)
            {
                if (board.IsInCheck()) // Faster than IsInCheckmate()
                {
                    return -INF;
                }
                return 0f;
            }

            OrderMoves(moves, board);

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                float value = -DepthSearch(board, depth - 1, maxDepth, -beta, -alpha, timer);
                board.UndoMove(move);

                if (value >= beta)
                {
                    return beta;
                }
                if (value > alpha)
                {
                    alpha = value;
                    if (depth == maxDepth)
                    {
                        bestMove = move;
                    }
                }
            }
            return alpha;
        }

        public float SearchAllCaptures(Board board, float alpha, float beta, Timer timer)
        {
            Move[] captureMoves = board.GetLegalMoves(capturesOnly: true);
            OrderMoves(captureMoves, board);

            float evaluation = EvaluatePosition(board);
            if (evaluation > beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, evaluation);

            foreach (Move move in captureMoves)
            {
                board.MakeMove(move);
                evaluation = -SearchAllCaptures(board, -beta, -alpha, timer);
                board.UndoMove(move);

                if (evaluation >= beta)
                {
                    return beta;
                }

                alpha = Math.Max(alpha, evaluation);
            }

            return alpha;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        depthSearcher.DepthSearch(board, MAXDEPTH, MAXDEPTH, float.NegativeInfinity, float.PositiveInfinity, timer);
        return depthSearcher.GetMove(board);
    }
}