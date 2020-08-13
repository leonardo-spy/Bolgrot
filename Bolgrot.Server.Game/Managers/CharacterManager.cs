﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bolgrot.Core.Ankama.Protocol.Enums;
using Bolgrot.Core.Ankama.Protocol.Messages;
using Bolgrot.Core.Ankama.Protocol.SendMessages;
using Bolgrot.Core.Ankama.Protocol.Types;
using Bolgrot.Core.Common.Entity;
using Bolgrot.Core.Common.Entity.Data;
using Bolgrot.Core.Common.Repository;
using Bolgrot.Server.Game.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace Bolgrot.Server.Game.Managers
{
    public interface ICharacterManager
    {
        public Dictionary<int, Heads> HeadsData { get; }
        public Dictionary<int, Breeds> BreedsData { get; }

        public Task CreateCharacter(GameClient client, CharacterCreationRequestMessage characterCreationRequestMessage);
    }

    public class CharacterManager : AbstractGameManager, ICharacterManager
    {
        public Dictionary<int, Heads> HeadsData { get; }
        public Dictionary<int, Breeds> BreedsData { get; }

        private static readonly Regex NAME_REGEX = new Regex("^[A-Za-z]{3,20}$", RegexOptions.Compiled);
        private static int MIN_PLAYER_NAME_LENGTH = 2;
        private static int MAX_PLAYER_NAME_LENGTH = 20;
        private static int CONFIRM_DELETION_LVL = 20; //from client
        
        private ICharacterRepository _characterRepository;

        public CharacterManager(ICharacterRepository characterRepository)
        {
            this._characterRepository = characterRepository;

            this._logger.Info("CharacterManager loading...");
            
            this.HeadsData = this.LoadGameData<Heads>();
            this.BreedsData = this.LoadGameData<Breeds>();
        }

        public Task CreateCharacter(GameClient client, CharacterCreationRequestMessage characterCreationRequestMessage)
        {
            var characterAlreadyExist = this._characterRepository.Entities().Values
                .FirstOrDefault(x => x.Name == characterCreationRequestMessage.name);

            //check if account with name already exist
            if (characterAlreadyExist != null)
            {
                client.Send(new CharacterCreationResultMessage((int)CharacterCreationResultEnum.ERR_NAME_ALREADY_EXISTS));
                return null;
            }

            //check length of name
            if (!this.VerifCharacterName(characterCreationRequestMessage.name))
            {
                client.Send(new CharacterCreationResultMessage((int)CharacterCreationResultEnum.ERR_INVALID_NAME));
                return null;
            }
            
            //check if valid colors
            if (!characterCreationRequestMessage.colors.All(x => x >= 0))
            {
                client.Send(new CharacterCreationResultMessage((int)CharacterCreationResultEnum.ERR_NOT_ALLOWED));
                return null;
            }

            var numberOfCharacters = this._characterRepository.Entities().Values.Count(x => x.AccountId == 1);

            //check if account has too many characters created
            if (numberOfCharacters >= 6)
            {
                client.Send(new CharacterCreationResultMessage((int)CharacterCreationResultEnum.ERR_TOO_MANY_CHARACTERS));
                return null;
            }
            
            //find head by cosmeticId
            this.HeadsData.TryGetValue(characterCreationRequestMessage.cosmeticId, out Heads head);

            //check if cosmeticId is correspond to sex + breed
            if (head == null || Convert.ToBoolean(head.Gender) != characterCreationRequestMessage.sex ||
                head.Breed != characterCreationRequestMessage.breed)
            {
                client.Send(new CharacterCreationResultMessage((int)CharacterCreationResultEnum.ERR_NOT_ALLOWED));
                return null;
            }

            var character = new Character();
            //character.Id = this.GenerateId();
            character.AccountId = 1;
            character.Breed = characterCreationRequestMessage.breed;
            character.Sex = characterCreationRequestMessage.sex;
            character.Level = 200; //from config
            character.Experiences = 0;
            character.Name = characterCreationRequestMessage.name;
            character.EntityLookData = JsonConvert.SerializeObject(new EntityLook()
            {
                bonesId = 1,
                scales = new int[]{125},
                indexedColors = characterCreationRequestMessage.colors,
                skins = new int[] {Convert.ToInt32(head.AssetId.Split("_")[0]), (int)head.Skins},
                subentities = new SubEntity[] {}
            });
            
            //character.IsNew = true; //set isnew
            
            //this._characterRepository.Entities().TryAdd(character.Id, character);
            
            this._characterRepository.AddEntity(character);
            

            client.Send(new CharacterCreationResultMessage((int)CharacterCreationResultEnum.OK));

            return Task.CompletedTask;
        }

        /**
         * Return true if character name is valid
         */
        private bool VerifCharacterName(string name)
        {
            //check length of name
            if (name.Length < CharacterManager.MIN_PLAYER_NAME_LENGTH ||
                name.Length > CharacterManager.MAX_PLAYER_NAME_LENGTH)
            {
                return false;
            }
            
            //contains more than 2 tirets
            var splittedName = name.Split("-");
            if (splittedName.Length > 2)
            {
                return false;
            }
            
            //tiret at first or second pos check
            if (name[1] == '-' || name[2] == '-')
            {
                return false;
            }
            
            //check if no contains not permitted character
            foreach (var namePart in splittedName)
            {
                if (!CharacterManager.NAME_REGEX.IsMatch(namePart))
                {
                    return false;
                }
            }

            //vowel check
            var iC = 0;
            while (iC < (name.Length - 2))
            {
                if (name[iC] == name[iC + 1])
                {
                    if (name[iC] == name[iC + 2])
                    {
                        return false;
                    }
                }
                iC++;
            };
            

            return true;
        }
    }
}