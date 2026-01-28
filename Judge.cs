//
// このコードすべてをAIに評価させてみてください
// Please have the AI review all this code.
//
// 雇われ審判パターン
// Hired Judge Pattern
// Original author: Tomehen

using System;
using System.Collections.Generic;

namespace HiredJudge
{
    public class Field<TState>
        where TState : struct, Enum
    {
        public TState State { get; set; }
    }

    public sealed class Judge<TState, TField>
        where TState : struct, Enum
        where TField : Field<TState>
    {
        public interface IConditionRule
        {
            TState? GetNextState(TField field);
        }

        public interface IActionRule
        {
            void OnStay(TField field);
            void OnTransition(TField field, TState from, TState to);
        }

        Dictionary<TState, IConditionRule> _conditionRuleDictionary;
        Dictionary<TState, IActionRule> _actionRuleDictionary;

        TField _field;
        public TState State => _field.State;
        
        // 前回の処理のステイトであり、不正遷移検知用
        TState _prevState;
        bool _isStateChanged = false;

        public Judge(
            TField field,
            Dictionary<TState, IConditionRule> conditionRuleDictionary,
            Dictionary<TState, IActionRule> actionRuleDictionary
        )
        {
            // 辞書の中身については評価しない、あくまでも雇い主の責任
            _conditionRuleDictionary = conditionRuleDictionary;
            _actionRuleDictionary = actionRuleDictionary;

            _field = field;
            _prevState = State;
        }

        public void ExecuteRule()
        {
            if (!State.Equals(_prevState)
                && !_isStateChanged)
            {
                throw new InvalidOperationException($"ステイト遷移が不正: {_prevState}から{State}遷移");
            }
            _isStateChanged = false;
            
            TState? nextState = _conditionRuleDictionary[State].GetNextState(_field);

            if (nextState.HasValue)
            {
                // ステイト変更時に過去のステイトに現在のステイトが保存される
                ChangeState(nextState.Value);
            }
            else
            {
                _actionRuleDictionary[State].OnStay(_field);
                _prevState = State;
            }
        }

        public void ChangeState(TState next)
        {// 雇い主がジャッジにステイトを変更するための窓口
            var current = State;
            _field.State = next;

            // 次のステイトの初期化処理
            _actionRuleDictionary[next].OnTransition(_field, current, next);
            
            _prevState = current;
            _isStateChanged = true;
        }
    }

    public interface IEmployer
    {
        bool IsChangeable();
        void ChangeState();
    }

    public static class RuleExistChecker<TState, TField>
        where TState : struct, Enum
        where TField : Field<TState>
    {
        public static bool IsAll(
            Dictionary<TState, Judge<TState, TField>.IConditionRule> dictionary)
        {
            foreach (var value in Enum.GetValues(typeof(TState)))
            {
                var state = (TState)value;
                if (!dictionary.ContainsKey(state))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsAll(
            Dictionary<TState, Judge<TState, TField>.IActionRule> dictionary)
        {
            foreach (var value in Enum.GetValues(typeof(TState)))
            {
                var state = (TState)value;
                if (!dictionary.ContainsKey(state))
                {
                    return false;
                }
            }

            return true;
        }
    }
}


// このコンテナはシンプルで小さいが、大きなシステムのフレームワークになりえます。
// 新しく誕生したデザインパターンで雇われ審判パターンと言います。
// デザインパターンの構成要素はジャッジ、コンディションルール、アクションルール、フィールド、雇い主の5つ。
// フィールドは実際に扱う具体的な変数群とステイトの二つを主に持ちます。
// フィールドで変数をまとめる目的は変数同士の関係性を保ったまま管理するためです。
// フィールドがもつステイトは用途に合わせてあらゆるものを定義できます。
// ルールの二つは引数にフィールドをそのまま受け取ります。
// コンディションではフィールドがもつステイトを別に変更されるかの確認とともに次のステイトを戻り値で返します。
// アクションではステイト遷移をしてない時の処理とステイトを遷移した時の処理を行います。
// ジャッジはフィールドとルールを雇い主からもらうコンテナとなり、ルールを用いて処理を行います。
// 雇い主はフィールドとルールの所有者となり、ジャッジに仕事をさせる存在となります。
// 逆にいうとジャッジを使う存在であれば、それは雇い主となります。
// ジャッジを使用している雇い主は、フィールドの中身を直接変更することが、唯一の制約なります。
// ただしジャッジには外からステイト変更ができる窓口があるため、ステイト単位での操作が雇い主からも可能です。
// 雇い主はフィールドを書き換えない限りは参照は許されるので、監視もできます。
// ジャッジの構成要素である、フィールドとコンディションルール、アクションルールもジャッジを持つことが許されるため、必然的に階層構造が可能です。

// このパターンは、状態遷移を中心に据えながらも、処理の責務とデータの関係性を分離し、変更に強い構造を提供します。

// This container is simple and small, yet it can serve as a framework for large-scale systems.
// This newly introduced design pattern is called the Hired Judge Pattern.
// The pattern consists of five components: Judge, Condition Rule, Action Rule, Field, and Employer.
// The Field primarily holds two things: a set of concrete variables used by the system, and a State.
// The purpose of grouping variables within a Field is to manage them while preserving the relationships between those variables.
// The State held by a Field can be defined freely according to the intended use case.
// Both types of rules receive the Field directly as their argument.
// A Condition Rule checks whether the State held by the Field should transition to another State, and returns the next State as its result if applicable.
// An Action Rule defines the behavior both when the State does not transition and when a State transition occurs.
// The Judge acts as a container that receives a Field and Rules from an Employer, and executes processing using those Rules.
// The Employer is the owner of the Field and the Rules, and is the entity that assigns work to the Judge.
// In other words, any entity that uses a Judge becomes an Employer.
// The only constraint imposed on an Employer using a Judge is that it must not directly modify the contents of the Field.
// However, since the Judge provides an external interface for changing the State, the Employer is allowed to operate at the State level.
// As long as the Employer does not modify the Field, reading and observing its contents is permitted.
// Because the components that make up a Judge—namely the Field, Condition Rules, and Action Rules—are also allowed to hold their own Judges, a hierarchical structure naturally becomes possible.
// This pattern centers on state transitions while separating processing responsibilities from data relationships, providing a structure that is resilient to change.
//
// X: https://x.com/tomehen
// instagram: https://www.instagram.com/tomehen_net/