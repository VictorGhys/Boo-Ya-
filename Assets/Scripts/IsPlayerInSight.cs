using System.Collections;
using System.Collections.Generic;
using BBUnity.Conditions;
using Pada1.BBCore;
using UnityEngine;


namespace BBUnity.Conditions
{
    [Condition("IsPlayerInSight")]
    [Help("Returns true if the player is inside the visioncone")]
    public class IsPlayerInSight : GOCondition
    {
        [InParam("npcBehavior")] private NPCBehavior _npcBehavior;
        [InParam("player")] private GameObject _player;

        public override bool Check()
        {
            if (!_npcBehavior || !_player)
                return false;

            if (_npcBehavior.CurrentState == NPCBehavior.State.Scared)
                return false;

            PlayerCharacter playerCharacter = _player.GetComponent<PlayerCharacter>();

            if (_npcBehavior.CurrentElement == NPCBehavior.ConeElement.Player
                && playerCharacter._isInvisible == false)
            {
                if (!LevelManager.Instance.AreLightsOnInRoom(_npcBehavior.RoomId))
                    return false;

                if (_npcBehavior.CurrentState != NPCBehavior.State.Scared && 
                    _npcBehavior.CurrentState != NPCBehavior.State.Terrified)
                {
                    _npcBehavior.InteruptSearch();
                    _npcBehavior.IncreaseScareMeter(30);
                    _npcBehavior.PlayGasp();
                    _npcBehavior.PlayJumpAnimation();
                }

                _npcBehavior.CurrentElement = NPCBehavior.ConeElement.None;
                return true;
            }

            _npcBehavior.CurrentElement = NPCBehavior.ConeElement.None;
            return false;
        }
    }
}

