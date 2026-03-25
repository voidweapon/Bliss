using System;
using System.Collections.Generic;
using System.Text;

namespace Bliss.Gameplay.GameplayTags
{
    /// <summary>
    /// Enumerates the list of supported query expression types.
    /// </summary>
    public enum EGameplayTagQueryExprType : byte
    {
          Undefined = 0,
          AnyTagsMatch,
          AllTagsMatch,
          NoTagsMatch,
          AnyExprMatch,
          AllExprMatch,
          NoExprMatch,
    }

    public enum EGameplayTagQueryStreamVersion
    {
        InitialVersion = 0,

        // -----<new versions can be added before this line>-------------------------------------------------
        // - this needs to be the last line (see note below)
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    /// <summary>
    /// An FGameplayTagQuery is a logical query that can be run against an FGameplayTagContainer.  A query that succeeds is said to "match".
    /// Queries are logical expressions that can test the intersection properties of another tag container (all, any, or none), or the matching state of a set of sub-expressions
    /// (all, any, or none). This allows queries to be arbitrarily recursive and very expressive.  For instance, if you wanted to test if a given tag container contained tags 
    /// ((A && B) || (C)) && (!D), you would construct your query in the form ALL( ANY( ALL(A,B), ALL(C) ), NONE(D) )
    /// 
    /// You can expose the query structs to Blueprints and edit them with a custom editor, or you can construct them natively in code. 
    /// 
    /// Example of how to build a query via code:
    ///	FGameplayTagQuery Q;
    ///	Q.BuildQuery(
    ///		FGameplayTagQueryExpression()
    /// 		.AllTagsMatch()
    ///		.AddTag(FGameplayTag::RequestGameplayTag(FName(TEXT("Animal.Mammal.Dog.Corgi"))))
    ///		.AddTag(FGameplayTag::RequestGameplayTag(FName(TEXT("Plant.Tree.Spruce"))))
    ///		);
    /// 
    /// Queries are internally represented as a byte stream that is memory-efficient and can be evaluated quickly at runtime.
    /// Note: these have an extensive details and graph pin customization for editing, so there is no need to expose the internals to Blueprints.
    ///
    /// </summary>
    public partial class GameplayTagQuery
    {
        public static GameplayTagQuery EmptyQuery = default;

        public GameplayTagQuery()
        {
            TokenStreamVersion = (byte)EGameplayTagQueryStreamVersion.LatestVersion;
            TagDictionary = new List<GameplayTag>();
            QueryTokenStream = new List<byte>();
            UserDescription = String.Empty;
            AutoDescription = String.Empty;
        }
        
        /// <summary>
        /// Versioning for future token stream protocol changes.
        /// <see cref="EGameplayTagQueryStreamVersion"/>
        /// </summary>
        private int TokenStreamVersion;

        /// <summary>
        /// List of tags referenced by this entire query. Token stream stored indices into this list.
        /// </summary>
    #if UNITY_2017_1_OR_NEWER
        [UnityEngine.SerializeField]
    #endif
        private List<GameplayTag> TagDictionary;

        /// <summary>
        /// Stream representation of the actual hierarchical query
        /// </summary>
    #if UNITY_2017_1_OR_NEWER
        [UnityEngine.SerializeField]
    #endif
        private List<byte> QueryTokenStream;
        
        /// <summary>
        /// User-provided string describing the query
        /// </summary>
    #if UNITY_2017_1_OR_NEWER
        [UnityEngine.SerializeField]
    #endif
        private string UserDescription;
        
        /// <summary>
        /// Auto-generated string describing the query
        /// </summary>
    #if UNITY_2017_1_OR_NEWER
        [UnityEngine.SerializeField]
    #endif
        private string AutoDescription;

        public int GetQueryTokenStreamLength()
        {
            return QueryTokenStream.Count;
        }
        
        public bool GetToken(int index, out byte token)
        {
            token = 0;
            if (index < QueryTokenStream.Count)
            {
                token = QueryTokenStream[index];
                return true;
            }

            return false;
        }
        
        public GameplayTag GetTagFromIndex(int index)
        {
            return TagDictionary[index];
        }
        

        /// <summary>
        /// Replaces existing tags with passed in tags. Does not modify the tag query expression logic. Useful when you need to cache off and update often used query. Must use same sized tag container!
        /// </summary>
        /// <param name="tags"></param>
        /// <exception cref="ArgumentException"></exception>
        public void ReplaceTagsFast(GameplayTagContainer tags)
        {
            if (tags.Num() != TagDictionary.Count)
            {
                throw new ArgumentException($" Must use same sized tag container!", nameof(tags));
            }
            TagDictionary.Clear();
            TagDictionary.AddRange(tags.GameplayTags);
        }
        
        /// <summary>
        /// Replaces existing tags with passed in tag. Does not modify the tag query expression logic. Useful when you need to cache off and update often used query.
        /// </summary>
        /// <param name="tag"></param>
        public void ReplaceTagFast(ref GameplayTag tag)
        {
            if (1 != TagDictionary.Count)
            {
                throw new ArgumentException($" Must use same sized tag container!", nameof(tag));
            }
            
            TagDictionary.Clear();
            TagDictionary.Add(tag);
        }
        
        /// <summary>
        /// Returns true if the given tags match this query, or false otherwise.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public bool Matches(GameplayTagContainer tags)
        {
            var evaluator = new QueryEvaluator()
            {
                Query = this
            };
            return evaluator.Eval(tags);
        }

        /// <summary>
        /// Returns true if this query is empty, false otherwise.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return QueryTokenStream.Count == 0;
        }

        /// <summary>
        /// Resets this query to its default empty state.
        /// </summary>
        public void Clear()
        {
            TokenStreamVersion = 0;
            QueryTokenStream.Clear();
            TagDictionary.Clear();
            UserDescription = string.Empty;
            AutoDescription = string.Empty;
        }

        /// <summary>
        /// Creates this query with the given root expression.
        /// </summary>
        public void Build(GameplayTagQueryExpression rootQueryExpr, string description)
        {
            TokenStreamVersion = (int)EGameplayTagQueryStreamVersion.LatestVersion;
            UserDescription = description;
            
            // Reserve size here is arbitrary, goal is to minimizing reallocs while being respectful of mem usage
            QueryTokenStream.Clear();
            TagDictionary.Clear();

            // add stream version first
            QueryTokenStream.Capacity = 128;
            QueryTokenStream.Add((byte)TokenStreamVersion);
            
            // emit the query
            QueryTokenStream.Add(1);// true to indicate is has a root expression
            rootQueryExpr.EmitTokens(QueryTokenStream, TagDictionary);
        }
        

        /// <summary>
        /// Static function to assemble and return a query.
        /// </summary>
        /// <param name="rootQueryExpr"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static GameplayTagQuery BuildQuery(GameplayTagQueryExpression rootQueryExpr, string description = null)
        {
            GameplayTagQuery query = new GameplayTagQuery();
            query.Build( rootQueryExpr, description);
            return query;
        }

        /// <summary>
        /// Builds a FGameplayTagQueryExpression from this query.
        /// </summary>
        /// <param name="outExpr"></param>
        public GameplayTagQueryExpression GetQueryExpr()
        {
            QueryEvaluator evaluator = new QueryEvaluator
            {
                Query = this
            };
            return evaluator.Read();
        }
        
        /// <summary>
        /// Returns description string.
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            return string.IsNullOrEmpty(UserDescription) ? AutoDescription : UserDescription;
        }

        /// <summary>
        /// Gets the explicit list of all unique gameplay tags referenced by the query.
        /// </summary>
        /// <returns></returns>
        public List<GameplayTag> GetGameplayTagArray()
        {
            return TagDictionary;
        }


        public static GameplayTagQuery MakeQuery_MatchAnyTags(GameplayTagContainer tags)
        {
            return BuildQuery(
                new GameplayTagQueryExpression()
                    .AnyTagsMatch()
                    .AddTags(tags)
                );
        }
        public static GameplayTagQuery MakeQuery_MatchAllTags(GameplayTagContainer tags)
        {
            return BuildQuery(
                new GameplayTagQueryExpression()
                    .AnyTagsMatch()
                    .AddTags(tags)
                );
        }
        public static GameplayTagQuery MakeQuery_MatchNoTags(GameplayTagContainer tags)
        {
            return BuildQuery(
                new GameplayTagQueryExpression()
                    .NoTagsMatch()
                    .AddTags(tags)
            );
        }
        public static GameplayTagQuery MakeQuery_MatchTag(GameplayTagContainer tags)
        {
            return BuildQuery(
                new GameplayTagQueryExpression()
                    .AllTagsMatch()
                    .AddTags(tags)     
            );
        }
    }

    public class GameplayTagQueryExpression
    {
        /// <summary>
        /// Which type of expression this is.
        /// </summary>
        internal EGameplayTagQueryExprType ExprType;

        /// <summary>
        /// Expression list, for expression types that need it
        /// </summary>
        private List<GameplayTagQueryExpression> ExprSet;

        /// <summary>
        /// Tag list, for expression types that need it 
        /// </summary>0
        private List<GameplayTag> TagSet;

        public GameplayTagQueryExpression()
        {
            ExprSet = new List<GameplayTagQueryExpression>();
            TagSet = new List<GameplayTag>();
        }
        
        public GameplayTagQueryExpression AnyTagsMatch()
        {
            ExprType = EGameplayTagQueryExprType.AnyTagsMatch;
            return this;
        }
        
        public GameplayTagQueryExpression AllTagsMatch()
        {
            ExprType = EGameplayTagQueryExprType.AllTagsMatch;
            return this;
        }
        
        public GameplayTagQueryExpression NoTagsMatch()
        {
            ExprType = EGameplayTagQueryExprType.NoTagsMatch;
            return this;
        }
        
        public GameplayTagQueryExpression AnyExprMatch()
        {
            ExprType = EGameplayTagQueryExprType.AnyExprMatch;
            return this;
        }
        public GameplayTagQueryExpression AllExprMatch()
        {
            ExprType = EGameplayTagQueryExprType.AllExprMatch;
            return this;
        }
        public GameplayTagQueryExpression NoExprMatch()
        {
            ExprType = EGameplayTagQueryExprType.NoExprMatch;
            return this;
        }
        public GameplayTagQueryExpression AddTag(string tagName)
        {
            var tag = GameplayTagsManager.Instance.RequestGameplayTag(tagName);
            return AddTag(ref tag);
        }
        public GameplayTagQueryExpression AddTag(ref  GameplayTag tag)
        {
            CheckUseTagSet();
            TagSet.Add(tag);
            return this;
        }
        
        public GameplayTagQueryExpression AddTags(GameplayTagContainer  tags)
        {
            CheckUseTagSet();
            TagSet.AddRange(tags.GameplayTags);
            return this;
        }
        public GameplayTagQueryExpression AddExpr(GameplayTagQueryExpression expr)
        {
            CheckUseExprSet();
            ExprSet.Add(expr);
            return this;
        }
        
        public void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary)
        {
            tokenStream.Add((byte)ExprType);

            switch (ExprType)
            {
                case EGameplayTagQueryExprType.AnyTagsMatch:
                case EGameplayTagQueryExprType.AllTagsMatch:
                case EGameplayTagQueryExprType.NoTagsMatch:
                    tokenStream.Add((byte)TagSet.Count);
                    foreach (var tag in this.TagSet)
                    {
                        int index = tagDictionary.IndexOf(tag);
                        if (index == -1)
                        {
                            tagDictionary.Add(tag);
                            index = tagDictionary.Count - 1;
                        }

                        if (index > 254)
                        {
                            throw new Exception("we reserve token 255 for internal use, so 254 is max unique tags");
                        }
                        tokenStream.Add((byte)index);
                    }
                    break;
                case EGameplayTagQueryExprType.AnyExprMatch:
                case EGameplayTagQueryExprType.AllExprMatch:
                case EGameplayTagQueryExprType.NoExprMatch:
                    tokenStream.Add((byte)ExprSet.Count);
                    foreach (var expression in ExprSet)
                    {
                        expression.EmitTokens(tokenStream, tagDictionary);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// Returns true if this expression uses the tag data.
        /// </summary>
        /// <returns></returns>
        public bool UsesTagSet()
        {
            return (ExprType == EGameplayTagQueryExprType.AllTagsMatch) ||
                   (ExprType == EGameplayTagQueryExprType.AnyTagsMatch) ||
                   (ExprType == EGameplayTagQueryExprType.NoTagsMatch);
        }

        /// <summary>
        /// Returns true if this expression uses the expression list data.
        /// </summary>
        /// <returns></returns>
        public bool UsesExprSet()
        {
            return (ExprType == EGameplayTagQueryExprType.AllExprMatch) ||
                   (ExprType == EGameplayTagQueryExprType.AnyExprMatch) ||
                   (ExprType == EGameplayTagQueryExprType.NoExprMatch);
        }


        private void CheckUseTagSet()
        {
            if (!UsesTagSet())
            {
                throw new ArgumentException($"this GameplayTagQueryExpression not use Tag");
            }
        }
        private void CheckUseExprSet()
        {
            if (!UsesExprSet())
            {
                throw new ArgumentException($"this GameplayTagQueryExpression not use expression");
            }
        }
    }

    /// <summary>
    /// Helper class to parse/eval query token streams.
    /// </summary>
    internal partial class QueryEvaluator
    {
        internal GameplayTagQuery Query;
        private int CurStreamIdx;
        private int Version;
        private bool bReadError;

        public bool Eval(GameplayTagContainer tags)
        {
            CurStreamIdx = 0;
            Version = GetToken();
            if (bReadError)
            {
                return false;
            }

            bool bRet = false;
            byte bHasRootExpression = GetToken();
            if (!bReadError && bHasRootExpression > 0)
            {
                bRet = EvalExpr(tags);
            }

            CheckParse();
            return bRet;
        }

        public GameplayTagQueryExpression Read()
        {
            var expr = new GameplayTagQueryExpression();
            CurStreamIdx = 0;

            if (Query.GetQueryTokenStreamLength() > 0)
            {
                Version = GetToken();
                if (!bReadError)
                {
                    bool bHasRootExpression = GetToken() > 0;
                    if (!bReadError && bHasRootExpression)
                    {
                        ReadExpr(expr);
                    }
                }
            }

            return expr;
        }

        private bool EvalAnyTagsMatch(GameplayTagContainer tags, bool bSkip)
        {
            bool bShortCircuit = bSkip;
            bool result = false;
            
            // parse tagset
            int numTags = GetToken();
            if (bReadError)
            {
                return false;
            }

            for (int i = 0; i < numTags; i++)
            {
                int tagIndex = GetToken();
                if (bReadError)
                {
                    return false;
                }

                if (!bShortCircuit)
                {
                    var tag = Query.GetTagFromIndex(tagIndex);
                    bool hasTag = tags.HasTag(ref tag);
                    if (hasTag)
                    {
                        // one match is sufficient for a true result!
                        bShortCircuit = true;
                        result = true;
                    }
                }
            }

            return result;
        }
        private bool EvalAllTagsMatch(GameplayTagContainer tags, bool bSkip)
        {
            bool bShortCircuit = bSkip;
            // assume true until proven otherwise
            bool result = true;
            
            // parse tagset
            int numTags = GetToken();
            if (bReadError)
            {
                return false;
            }
            
            for (int i = 0; i < numTags; i++)
            {
                int tagIndex = GetToken();
                if (bReadError)
                {
                    return false;
                }

                if (!bShortCircuit)
                {
                    var tag = Query.GetTagFromIndex(tagIndex);
                    bool hasTag = tags.HasTag(ref tag);
                    if (!hasTag)
                    {
                        // one failed match is sufficient for a false result
                        bShortCircuit = true;
                        result = false;
                    }
                }
            }

            return result;
        }
        private bool EvalNoTagsMatch(GameplayTagContainer tags, bool bSkip)
        {
            bool bShortCircuit = bSkip;
            // assume true until proven otherwise
            bool result = true;
            
            // parse tagset
            int numTags = GetToken();
            if (bReadError)
            {
                return false;
            }
            
            for (int i = 0; i < numTags; i++)
            {
                int tagIndex = GetToken();
                if (bReadError)
                {
                    return false;
                }

                if (!bShortCircuit)
                {
                    var tag = Query.GetTagFromIndex(tagIndex);
                    bool hasTag = tags.HasTag(ref tag);
                    if (hasTag)
                    {
                        // one match is sufficient for a false result
                        bShortCircuit = true;
                        result = false;
                    }
                }
            }

            return result;
        }
        
        private bool EvalAnyExprMatch(GameplayTagContainer tags, bool bSkip)
        {
            bool bShortCircuit = bSkip;
            // assume false until proven otherwise
            bool result = false;
            
            // parse tagset
            int numExprs = GetToken();
            if (bReadError)
            {
                return false;
            }

            for (int i = 0; i < numExprs; i++)
            {
                bool bExprResult = EvalExpr(tags, bShortCircuit);
                if (!bShortCircuit)
                {
                    if (bExprResult)
                    {
                        // one match is sufficient for true result
                        bShortCircuit = true;
                        result = true;
                    }
                }
            }

            return result;
        }
        private bool EvalAllExprMatch(GameplayTagContainer tags, bool bSkip)
        {
            bool bShortCircuit = bSkip;
            // assume true until proven otherwise
            bool result = true;
            
            // parse tagset
            int numExprs = GetToken();
            if (bReadError)
            {
                return false;
            }

            for (int i = 0; i < numExprs; i++)
            {
                bool bExprResult = EvalExpr(tags, bShortCircuit);
                if (!bShortCircuit)
                {
                    if (!bExprResult)
                    {
                        // one fail is sufficient for false result
                        bShortCircuit = true;
                        result = false;
                    }
                }
            }

            return result;
        }
        private bool EvalNoExprMatch(GameplayTagContainer tags, bool bSkip)
        {
            bool bShortCircuit = bSkip;
            // assume true until proven otherwise
            bool result = true;
            
            // parse tagset
            int numExprs = GetToken();
            if (bReadError)
            {
                return false;
            }

            for (int i = 0; i < numExprs; i++)
            {
                bool bExprResult = EvalExpr(tags, bShortCircuit);
                if (!bShortCircuit)
                {
                    if (bExprResult)
                    {
                        // one fail is sufficient for false result
                        bShortCircuit = true;
                        result = false;
                    }
                }
            }

            return result;
        }
        
        private bool EvalExpr(GameplayTagContainer tags, bool bSkip = false)
        {
            var  exprType = (EGameplayTagQueryExprType)GetToken();
            if (bReadError)
            {
                return false;
            }

            switch (exprType)
            {
                case EGameplayTagQueryExprType.AnyTagsMatch:
                    return EvalAnyTagsMatch(tags, bSkip);
                case EGameplayTagQueryExprType.AllTagsMatch:
                    return EvalAllTagsMatch(tags, bSkip);
                case EGameplayTagQueryExprType.NoTagsMatch:
                    return EvalNoTagsMatch(tags, bSkip);
                
                case EGameplayTagQueryExprType.AnyExprMatch:
                    return EvalAnyExprMatch(tags, bSkip);
                case EGameplayTagQueryExprType.AllExprMatch:
                    return EvalAllExprMatch(tags, bSkip);
                case EGameplayTagQueryExprType.NoExprMatch:
                    return EvalNoExprMatch(tags, bSkip);
            }

            return false;
        }
        
        private void ReadExpr(GameplayTagQueryExpression expr)
        {
            expr.ExprType = (EGameplayTagQueryExprType)GetToken();

            if (expr.UsesTagSet())
            {
                // parse tag set
                int numTags = GetToken();
                if (bReadError)
                {
                    return;
                }

                for (int i = 0; i < numTags; i++)
                {
                    int tagIndex = GetToken();
                    if (bReadError)
                    {
                        return;
                    }

                    GameplayTag tag = Query.GetTagFromIndex(tagIndex);
                    expr.AddTag(ref tag);
                }
            }
            else
            {
                // parse expr set
                int numExprs = GetToken();
                if (bReadError)
                {
                    return;
                }

                for (int i = 0; i < numExprs; i++)
                {
                    var childExpr = new GameplayTagQueryExpression();
                    ReadExpr(childExpr);
                    expr.AddExpr(childExpr);
                }
            }
        }

        private byte GetToken()
        {
            if (Query.GetToken(CurStreamIdx, out var token))
            {
                CurStreamIdx++;
                return token;
            }

            GameplayTagsManager.Log("Error parsing FGameplayTagQuery!");
            bReadError = true;
            return 0;
        }

        private void CheckParse()
        {
            if (CurStreamIdx != Query.GetQueryTokenStreamLength())
            {
                throw new Exception($"Error parsing FGameplayTagQuery!");
            }
        }
    }


    # region Editor Class
    #if UNITY_EDITOR

    [System.Serializable]
    public partial class GameplayTagQuery 
    {
        public void BuildFromEditableQuery(EditableGameplayTagQuery editableQuery)
        {
            QueryTokenStream.Clear();
            TagDictionary.Clear();
            UserDescription = editableQuery.UserDescription;
            
            QueryTokenStream.Add((byte)EGameplayTagQueryStreamVersion.LatestVersion);
            editableQuery.EmitTokens(QueryTokenStream, TagDictionary);
        }

        public EditableGameplayTagQuery CreateEditableQuery()
        {
            QueryEvaluator evaluator = new QueryEvaluator
            {
                Query = this
            };

            return evaluator.CreateEditableQuery();
        }
    }

    internal partial class QueryEvaluator
    {
        public EditableGameplayTagQuery CreateEditableQuery()
        {
            CurStreamIdx = 0;
            EditableGameplayTagQuery editableGameplayTagQuery = new EditableGameplayTagQuery();

            if (!Query.IsEmpty())
            {
                Version = GetToken();
                if (!bReadError)
                {
                    byte bHasRootExpression = GetToken();
                    if (!bReadError && bHasRootExpression == 1)
                    {
                        editableGameplayTagQuery.RootExpression = ReadEditableQueryExpr();
                    }
                }
                System.Diagnostics.Debug.Assert(CurStreamIdx == Query.GetQueryTokenStreamLength());
            }

            editableGameplayTagQuery.UserDescription = Query.GetDescription();
            
            return editableGameplayTagQuery;
        }
        
        EditableGameplayTagQueryExpression ReadEditableQueryExpr()
        {
            EGameplayTagQueryExprType  exprType = (EGameplayTagQueryExprType) GetToken();
            if (bReadError)
            {
                return null;
            }

            EditableGameplayTagQueryExpression newExpr = null;
            switch (exprType)
            {
                case EGameplayTagQueryExprType.AnyTagsMatch:
                    newExpr = new EditableGameplayTagQueryExpression_AnyTagsMatch();
                    break;
                case EGameplayTagQueryExprType.AllTagsMatch:
                    newExpr = new EditableGameplayTagQueryExpression_AllTagsMatch();
                    break;
                case EGameplayTagQueryExprType.NoTagsMatch:
                    newExpr = new EditableGameplayTagQueryExpression_NoTagsMatch();
                    break;
                case EGameplayTagQueryExprType.AnyExprMatch:
                    newExpr = new UEditableGameplayTagQueryExpression_AnyExprMatch();
                    break;
                case EGameplayTagQueryExprType.AllExprMatch:
                    newExpr = new UEditableGameplayTagQueryExpression_AllExprMatch();
                    break;
                case EGameplayTagQueryExprType.NoExprMatch:
                    newExpr = new UEditableGameplayTagQueryExpression_NoExprMatch();
                    break;
                default:
                    return null;
                    
            }

            switch (exprType)
            {
                case EGameplayTagQueryExprType.AnyTagsMatch:
                case EGameplayTagQueryExprType.AllTagsMatch:
                case EGameplayTagQueryExprType.NoTagsMatch:
                    ReadEditableQueryTags(newExpr);
                    break;
                case EGameplayTagQueryExprType.AnyExprMatch:
                case EGameplayTagQueryExprType.AllExprMatch:
                case EGameplayTagQueryExprType.NoExprMatch:
                    ReadEditableQueryExprList(newExpr);
                    break;
            }
            return newExpr;
        }

        void ReadEditableQueryTags(EditableGameplayTagQueryExpression editableQueryExpr)
        {
            GameplayTagContainer tagContainer = null;
            if (editableQueryExpr is EditableGameplayTagQueryExpression_AnyTagsMatch anyTagsMatch)
            {
                tagContainer = anyTagsMatch.Tags;
            }
            else if (editableQueryExpr is EditableGameplayTagQueryExpression_AllTagsMatch allTagsMatch)
            {
                tagContainer = allTagsMatch.Tags;
            }
            else if (editableQueryExpr is EditableGameplayTagQueryExpression_NoTagsMatch noTagsMatch)
            {
                tagContainer = noTagsMatch.Tags;
            }
            System.Diagnostics.Debug.Assert(tagContainer != null);

            if (tagContainer != null)
            {
                int numTags = GetToken();
                if (bReadError)
                {
                    return;
                }

                for (int i = 0; i < numTags; i++)
                {
                    int tagIdx = GetToken();
                    if (bReadError)
                    {
                        return;
                    }

                    GameplayTag tag = Query.GetTagFromIndex(tagIdx);
                    tagContainer.AddTag(ref tag);
                }
            }
        }

        void ReadEditableQueryExprList(EditableGameplayTagQueryExpression editableQueryExpr)
        {
            List<EditableGameplayTagQueryExpression> exprList = null;
            if (editableQueryExpr is UEditableGameplayTagQueryExpression_AnyExprMatch anyExprMatch)
            {
                exprList = anyExprMatch.Expressions;
            }
            if (editableQueryExpr is UEditableGameplayTagQueryExpression_AllExprMatch allExprMatch)
            {
                exprList = allExprMatch.Expressions;
            }
            if (editableQueryExpr is UEditableGameplayTagQueryExpression_NoExprMatch noExprMatch)
            {
                exprList = noExprMatch.Expressions;
            }
            System.Diagnostics.Debug.Assert(exprList != null);

            if (exprList != null)
            {
                int numExpr = GetToken();
                if (bReadError)
                {
                    return;
                }

                for (int i = 0; i < numExpr; i++)
                {
                    EditableGameplayTagQueryExpression newExpr = ReadEditableQueryExpr();
                    exprList.Add(newExpr);
                }
            }
            
        }
    }
    
    /** 
     * This is an editor-only representation of a query, designed to be editable with a typical property window. 
     * To edit a query in the editor, an FGameplayTagQuery is converted to a set of UObjects and edited,  When finished,
     * the query struct is rewritten and these UObjects are discarded.
     * This query representation is not intended for runtime use.
     */
    [System.Serializable]
    public class EditableGameplayTagQuery
    {
        public string UserDescription;
        public EditableGameplayTagQueryExpression RootExpression;
        
        public void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary, StringBuilder debugString = null)
        {
            if (debugString != null)
            {
                debugString.Clear();
            }

            if (RootExpression != null)
            {
                tokenStream.Add(1);
                RootExpression.EmitTokens(tokenStream, tagDictionary, debugString);
            }
            else
            {
                tokenStream.Add(0);
                debugString?.Append("undefined");
            }
        }
        
    }

    public class EditableGameplayTagQueryExpression
    {
        public virtual void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary, StringBuilder debugString = null)
        {
            
        }
        
        protected void EmitTagTokens(
            GameplayTagContainer tagsToEmit, 
            List<byte> tokenStream,
            List<GameplayTag> tagDictionary, 
            StringBuilder debugString = null)
        {
            byte numTags = (byte)tagsToEmit.Num();
            tokenStream.Add(numTags);

            bool bFirstTag = true;
            
            foreach (var tag in tagsToEmit.GameplayTags)
            {
                int tagIdx = tagDictionary.IndexOf(tag);
                if (tagIdx == -1)
                {
                    tagDictionary.Add(tag);
                    tagIdx = tagDictionary.Count - 1;
                }
                System.Diagnostics.Debug.Assert(tagIdx <= 255);
                
                tokenStream.Add((byte)tagIdx);
                
                if (debugString != null)
                {
                    if (bFirstTag == false)
                    {
                        debugString.Append(",");
                    }

                    debugString.Append(" ");
                    debugString.Append(tag.ToString());
                }
                bFirstTag = false;
            }
        }
        
        protected void EmitExprListTokens(
            List<EditableGameplayTagQueryExpression> exprList, 
            List<byte> tokenStream,
            List<GameplayTag> tagDictionary, 
            StringBuilder debugString = null)
        {
            byte numExprs = (byte)exprList.Count;
            tokenStream.Add(numExprs);
            
            bool bFirstTag = true;
            
            foreach (var expr in exprList)
            {
                if (debugString != null)
                {
                    if (bFirstTag == false)
                    {
                        debugString.Append(",");
                    }
                    debugString.Append(" ");
                }

                if (expr != null)
                {
                    expr.EmitTokens(tokenStream, tagDictionary, debugString);
                }
                else
                {
                    // null expression
                    tokenStream.Add((byte)EGameplayTagQueryExprType.Undefined);
                    debugString?.Append("undefined");
                }
                
                bFirstTag = false;
            }
        }
    }

    public class EditableGameplayTagQueryExpression_AnyTagsMatch : EditableGameplayTagQueryExpression
    {
        public GameplayTagContainer Tags;

        public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary,  StringBuilder debugString = null)
        {
            tokenStream.Add(((byte)EGameplayTagQueryExprType.AnyTagsMatch));

            if (debugString != null)
            {
                debugString.Append(" ANY(");
            }
            
            EmitTagTokens(Tags, tokenStream, tagDictionary, debugString);
            
            if (debugString != null)
            {
                debugString.Append(" )");
            }
        }
    }

    public class EditableGameplayTagQueryExpression_AllTagsMatch: EditableGameplayTagQueryExpression
    {
        public GameplayTagContainer Tags;
        
        public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary,  StringBuilder debugString = null)
        {
            tokenStream.Add(((byte)EGameplayTagQueryExprType.AllTagsMatch));

            if (debugString != null)
            {
                debugString.Append(" ALL(");
            }
            
            EmitTagTokens(Tags, tokenStream, tagDictionary, debugString);
            
            if (debugString != null)
            {
                debugString.Append(" )");
            }
        }
    }

    public class EditableGameplayTagQueryExpression_NoTagsMatch: EditableGameplayTagQueryExpression
    {
        public GameplayTagContainer Tags;
        
        public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary,  StringBuilder debugString = null)
        {
            tokenStream.Add(((byte)EGameplayTagQueryExprType.NoTagsMatch));

            if (debugString != null)
            {
                debugString.Append(" NONE(");
            }
            
            EmitTagTokens(Tags, tokenStream, tagDictionary, debugString);
            
            if (debugString != null)
            {
                debugString.Append(" )");
            }
        }
    }

    public class UEditableGameplayTagQueryExpression_AnyExprMatch: EditableGameplayTagQueryExpression
    {
        public List<EditableGameplayTagQueryExpression> Expressions;
        
        public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary,  StringBuilder debugString = null)
        {
            tokenStream.Add(((byte)EGameplayTagQueryExprType.AnyExprMatch));

            if (debugString != null)
            {
                debugString.Append(" ANY_EXP(");
            }
            
            EmitExprListTokens(Expressions, tokenStream, tagDictionary, debugString);
            
            if (debugString != null)
            {
                debugString.Append(" )");
            }
        }
    }

    public class UEditableGameplayTagQueryExpression_AllExprMatch: EditableGameplayTagQueryExpression
    {
        public List<EditableGameplayTagQueryExpression> Expressions;
        
        public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary,  StringBuilder debugString = null)
        {
            tokenStream.Add(((byte)EGameplayTagQueryExprType.AllExprMatch));

            if (debugString != null)
            {
                debugString.Append(" ALL_EXP(");
            }
            
            EmitExprListTokens(Expressions, tokenStream, tagDictionary, debugString);
            
            if (debugString != null)
            {
                debugString.Append(" )");
            }
        }
    }

    public class UEditableGameplayTagQueryExpression_NoExprMatch: EditableGameplayTagQueryExpression
    {
        public List<EditableGameplayTagQueryExpression> Expressions;
        
        public override void EmitTokens(List<byte> tokenStream, List<GameplayTag> tagDictionary,  StringBuilder debugString = null)
        {
            tokenStream.Add(((byte)EGameplayTagQueryExprType.NoExprMatch));

            if (debugString != null)
            {
                debugString.Append(" NONE_EXP(");
            }
            
            EmitExprListTokens(Expressions, tokenStream, tagDictionary, debugString);
            
            if (debugString != null)
            {
                debugString.Append(" )");
            }
        }
    }
    #endif
    #endregion
}