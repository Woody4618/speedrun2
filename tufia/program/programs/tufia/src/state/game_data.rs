use std::{cmp, slice::SliceIndex};

use anchor_lang::prelude::*;
use solana_program::{program_pack::Sealed, pubkey};

use crate::{GameErrorCode, __private::__global::move_to_tile};

use super::player_data::{self, PlayerData};

const BOARD_SIZE_X: usize = 10;
const BOARD_SIZE_Y: usize = 10;

const STATE_EMPTY: u8 = 0;
const STATE_PLAYER: u8 = 1;
const STATE_ENEMY: u8 = 2;
const STATE_CHEST_GOLD: u8 = 3;
const STATE_CHEST_BLUE: u8 = 4;
const STATE_STAIRS: u8 = 5;

const ACTION_TYPE_MOVE: u8 = 0;
const ACTION_TYPE_FIGHT: u8 = 1;
const ACTION_TYPE_OPEN_CHEST: u8 = 2;
const ACTION_TYPE_RESET: u8 = 3;
const ACTION_TYPE_PLAYER_DIED: u8 = 4;

#[zero_copy(unsafe)]
#[repr(packed)]
#[derive(Default)]
pub struct GameAction {
    action_id: u64,  // 4
    action_type: u8, // 1
    from_x: u8,      // 1
    from_y: u8,      // 1
    to_x: u8,        // 1
    to_y: u8,        // 1
    tile: TileData,  // 32
    amount: u64,     // 4
}

#[account(zero_copy(unsafe))]
#[repr(packed)]
#[derive(Default)]
pub struct GameData {
    id_counter: u64,
    action_index: u64,
    pub data: [[TileData; BOARD_SIZE_X]; BOARD_SIZE_Y],
    pub total_wood_collected: u64,
    pub game_actions: [GameAction; 20],
    pub floor_id: u32,
    pub owner: Pubkey,
}

#[zero_copy(unsafe)]
#[repr(packed)]
#[derive(Default)]
pub struct TileData {
    pub tile_type: u8,
    pub tile_level: u32,
    pub tile_owner: Pubkey, // Could maybe be the avatar of the player building it? :thinking:
    pub tile_xp: u32,
    pub tile_damage: u32,
    pub tile_defence: u32,
    pub tile_armor: u32,
    pub tile_max_armor: u32,
    pub tile_health: u32,
    pub tile_max_health: u32,
}

#[derive(AnchorSerialize, AnchorDeserialize, Clone, Default)]
pub struct TileData2 {
    pub tile_type: u8,
    pub tile_level: u32,
    pub tile_owner: Pubkey, // Could maybe be the avatar of the player building it? :thinking:
    pub tile_xp: u32,
    pub tile_damage: u32,
    pub tile_defence: u32,
    pub tile_armor: u32,
    pub tile_max_armor: u32,
    pub tile_health: u32,
    pub tile_max_health: u32,
}

impl GameData {
    pub fn move_to_tile(
        &mut self,
        x: u64,
        y: u64,
        player: &mut PlayerData,
        spawn: bool,
    ) -> Result<()> {
        // Check if the player is on the board
        if x as usize >= BOARD_SIZE_X || y as usize >= BOARD_SIZE_Y {
            return Err(GameErrorCode::OutOfBounds.into());
        }

        let mut current_player_tile: Option<TileData> = None;
        let mut empty_slots: Vec<(usize, usize)> = Vec::new();
        let mut current_player_pos_x: usize = 0;
        let mut current_player_pos_y: usize = 0;

        let mut end_player_pos_x: usize = x as usize;
        let mut end_player_pos_y: usize = y as usize;

        let mut has_stairs: bool = false;

        let mut num_enemies: u32 = 0;

        for i in 0..BOARD_SIZE_X {
            for j in 0..BOARD_SIZE_Y {
                let tile: TileData = self.data[i][j];

                if tile.tile_type == STATE_STAIRS {
                    has_stairs = true;
                }
                if tile.tile_type == STATE_ENEMY {
                    num_enemies += 1;
                }

                if tile.tile_type == STATE_EMPTY {
                    empty_slots.push((i, j));
                } else if tile.tile_owner == player.authority && tile.tile_type == STATE_PLAYER {
                    current_player_tile = Some(tile);
                    current_player_pos_x = i;
                    current_player_pos_y = j;
                    let floorId = self.floor_id;
                    msg!("Found player tile {}{} floor: {}", i, j, floorId);
                } else if tile.tile_owner == player.authority
                    && tile.tile_type == STATE_PLAYER
                    && spawn
                {
                    return Err(GameErrorCode::PlayerAlreadyExists.into());
                }
            }
        }

        match current_player_tile {
            Some(mut tile) => {
                if x as usize == current_player_pos_x && y as usize == current_player_pos_y {
                    return Err(GameErrorCode::PlayerIsAlreadyOnThisTile.into());
                }

                let target_tile = self.data[x as usize][y as usize];
                msg!("Target tile: {} ", target_tile.tile_type);

                if target_tile.tile_type == STATE_CHEST_GOLD
                    || target_tile.tile_type == STATE_CHEST_BLUE
                {
                    let new_game_action = GameAction {
                        action_id: self.id_counter,
                        action_type: ACTION_TYPE_OPEN_CHEST,
                        from_x: current_player_pos_x as u8,
                        from_y: current_player_pos_x as u8,
                        to_x: x as u8,
                        to_y: x as u8,
                        tile: target_tile,
                        amount: 0,
                    };

                    self.add_new_game_action(new_game_action);

                    open_chest(
                        player,
                        &mut self.data,
                        current_player_pos_x,
                        current_player_pos_y,
                        x as usize,
                        y as usize,
                    )?;
                }

                msg!("Tile state: {} ", target_tile.tile_type);

                if target_tile.tile_type == STATE_EMPTY {
                    move_player(
                        &mut self.data,
                        current_player_pos_x,
                        current_player_pos_y,
                        x as usize,
                        y as usize,
                    )?;
                }

                if target_tile.tile_type == STATE_ENEMY {
                    fight_enemy(
                        player,
                        self,
                        //&mut self.data,
                        current_player_pos_x,
                        current_player_pos_y,
                        x as usize,
                        y as usize,
                    )?;
                    // Fight enemy
                }

                msg!("Fight player?");
                if target_tile.tile_type == STATE_PLAYER {
                    // Fight player
                    msg!("Fight player");
                    fight_enemy(
                        player,
                        self,
                        //&mut self.data,
                        current_player_pos_x,
                        current_player_pos_y,
                        x as usize,
                        y as usize,
                    )?;
                }

                if target_tile.tile_type == STATE_STAIRS {
                    // GO one floor down
                }
            }
            None => {
                if spawn {
                    if empty_slots.is_empty() {
                        return Err(GameErrorCode::BoardIsFull.into());
                    }

                    let mut rng = XorShift64 {
                        a: empty_slots.len() as u64 + player.health as u64,
                    };

                    let random_index = (rng.next() % (empty_slots.len() as u64)) as usize;
                    let random_empty_slot = empty_slots[random_index];
                    msg!(
                        "Player spawn at {} {}",
                        random_empty_slot.0,
                        random_empty_slot.1
                    );
                    end_player_pos_x = random_empty_slot.0 as usize;
                    end_player_pos_y = random_empty_slot.1 as usize;
                    empty_slots.remove(random_index);

                    self.data[random_empty_slot.0][random_empty_slot.1] = TileData {
                        tile_owner: player.authority.clone(),
                        tile_type: STATE_PLAYER,
                        tile_health: player.max_health,
                        tile_max_health: player.max_health,
                        tile_damage: player.damage,
                        tile_defence: player.defence,
                        tile_level: player.level,
                        tile_xp: player.xp,
                        ..Default::default()
                    };

                    current_player_tile = Some(self.data[random_empty_slot.0][random_empty_slot.1]);

                    // Spawn enemy
                    if (num_enemies < 6) {
                        if empty_slots.len() > 0 {
                            let random_index = (rng.next() % (empty_slots.len() as u64)) as usize;
                            let random_empty_slot = empty_slots[random_index];
                            msg!(
                                "Enemy spawn at {} {}",
                                random_empty_slot.0,
                                random_empty_slot.1
                            );

                            empty_slots.remove(random_index);

                            self.data[random_empty_slot.0][random_empty_slot.1] = TileData {
                                tile_owner: player.authority.clone(),
                                tile_type: STATE_ENEMY,
                                tile_health: player.current_floor as u32 + 5,
                                tile_max_health: player.current_floor as u32 + 5,
                                tile_damage: player.current_floor as u32 + 1,
                                tile_defence: player.current_floor as u32 + 1,
                                tile_armor: player.current_floor as u32 + 1,
                                tile_max_armor: player.current_floor as u32 + 1,
                                tile_level: player.current_floor as u32 + 1,
                                tile_xp: player.current_floor as u32 + 1,
                                ..Default::default()
                            };
                        }
                        if empty_slots.len() > 0 {
                            let random_index = (rng.next() % (empty_slots.len() as u64)) as usize;
                            let random_empty_slot = empty_slots[random_index];
                            msg!(
                                "Enemy spawn at {} {}",
                                random_empty_slot.0,
                                random_empty_slot.1
                            );

                            empty_slots.remove(random_index);

                            self.data[random_empty_slot.0][random_empty_slot.1] = TileData {
                                tile_owner: player.authority.clone(),
                                tile_type: STATE_ENEMY,
                                tile_health: player.current_floor as u32 + 5,
                                tile_max_health: player.current_floor as u32 + 5,
                                tile_damage: player.current_floor as u32 + 1,
                                tile_defence: player.current_floor as u32 + 1,
                                tile_armor: player.current_floor as u32 + 1,
                                tile_max_armor: player.current_floor as u32 + 1,
                                tile_level: player.current_floor as u32 + 1,
                                tile_xp: player.current_floor as u32 + 1,
                                ..Default::default()
                            };
                        }
                    }

                    // Spawn stairs
                    if empty_slots.len() > 0 && !has_stairs {
                        let random_index = (rng.next() % (empty_slots.len() as u64)) as usize;
                        let random_empty_slot = empty_slots[random_index];
                        msg!(
                            "Stairs spawn at {} {}",
                            random_empty_slot.0,
                            random_empty_slot.1
                        );

                        empty_slots.remove(random_index);

                        self.data[random_empty_slot.0][random_empty_slot.1] = TileData {
                            tile_owner: player.authority.clone(),
                            tile_type: STATE_STAIRS,
                            tile_health: player.current_floor as u32 + 5,
                            tile_max_health: player.current_floor as u32 + 5,
                            tile_damage: player.current_floor as u32 + 1,
                            tile_defence: player.current_floor as u32 + 1,
                            tile_armor: player.current_floor as u32 + 1,
                            tile_max_armor: player.current_floor as u32 + 1,
                            tile_level: player.current_floor as u32 + 1,
                            tile_xp: player.current_floor as u32 + 1,
                            ..Default::default()
                        };
                    }

                    let random_index = (rng.next() % (100)) as usize;

                    msg!("Random index chest: {}", random_index);
                    // Spawn Super chest
                    if empty_slots.len() > 0 && random_index > 70 {
                        let random_index = (rng.next() % (empty_slots.len() as u64)) as usize;
                        let random_empty_slot = empty_slots[random_index];
                        msg!(
                            "Player spawn at {} {}",
                            random_empty_slot.0,
                            random_empty_slot.1
                        );

                        empty_slots.remove(random_index);

                        self.data[random_empty_slot.0][random_empty_slot.1] = TileData {
                            tile_owner: player.authority.clone(),
                            tile_type: STATE_CHEST_BLUE,
                            tile_health: 0,
                            tile_damage: 0,
                            tile_defence: 0,
                            tile_level: 1,
                            tile_xp: 0,
                            ..Default::default()
                        };
                    }

                    let random_index = (rng.next() % (100)) as usize;

                    msg!("Random index chest: {}", random_index);
                    // Spawn Super chest
                    if empty_slots.len() > 0 {
                        let random_index = (rng.next() % (empty_slots.len() as u64)) as usize;
                        let random_empty_slot = empty_slots[random_index];
                        msg!(
                            "Player spawn at {} {}",
                            random_empty_slot.0,
                            random_empty_slot.1
                        );

                        empty_slots.remove(random_index);

                        self.data[random_empty_slot.0][random_empty_slot.1] = TileData {
                            tile_owner: player.authority.clone(),
                            tile_type: STATE_CHEST_GOLD,
                            tile_health: 0,
                            tile_damage: 0,
                            tile_defence: 0,
                            tile_level: 1,
                            tile_xp: 0,
                            ..Default::default()
                        };
                    }
                } else {
                    return Err(GameErrorCode::PlayerNotOnBoard.into());
                }
            }
        }

        if current_player_tile.is_none() {
            return Err(GameErrorCode::PlayerNotOnBoard.into());
        }

        let new_game_action = GameAction {
            action_id: self.id_counter,
            action_type: ACTION_TYPE_MOVE,
            from_x: current_player_pos_x as u8,
            from_y: current_player_pos_y as u8,
            to_x: x as u8,
            to_y: y as u8,
            tile: self.data[x as usize][y as usize],
            amount: 0,
        };

        self.add_new_game_action(new_game_action);

        let mut tile_data_clone: TileData2 = TileData2::default();
        tile_data_clone.tile_armor =
            self.data[end_player_pos_x as usize][end_player_pos_y as usize].tile_armor;
        tile_data_clone.tile_damage =
            self.data[end_player_pos_x as usize][end_player_pos_y as usize].tile_damage;
        tile_data_clone.tile_defence =
            self.data[end_player_pos_x as usize][end_player_pos_y as usize].tile_defence;
        tile_data_clone.tile_health =
            self.data[end_player_pos_x as usize][end_player_pos_y as usize].tile_health;
        tile_data_clone.tile_level =
            self.data[end_player_pos_x as usize][end_player_pos_y as usize].tile_level;
        tile_data_clone.tile_max_armor =
            self.data[end_player_pos_x as usize][end_player_pos_y as usize].tile_max_armor;
        tile_data_clone.tile_max_health =
            self.data[end_player_pos_x as usize][end_player_pos_y as usize].tile_max_health;
        tile_data_clone.tile_owner =
            self.data[end_player_pos_x as usize][end_player_pos_y as usize].tile_owner;
        tile_data_clone.tile_type =
            self.data[end_player_pos_x as usize][end_player_pos_y as usize].tile_type;
        tile_data_clone.tile_xp =
            self.data[end_player_pos_x as usize][end_player_pos_y as usize].tile_xp;

        player.tile_data = tile_data_clone;

        Ok(())
    }

    pub fn reset_game(&mut self) -> Result<()> {
        // TOOD: Make it so not everyone can just call this
        for i in 0..BOARD_SIZE_X {
            for j in 0..BOARD_SIZE_Y {
                self.data[i][j].tile_type = STATE_EMPTY;
            }
        }

        self.game_actions = [GameAction::default(); 20];
        self.action_index = 0;

        Ok(())
    }

    pub fn remove_player(&mut self, player: Pubkey) -> Result<()> {
        for i in 0..BOARD_SIZE_X {
            for j in 0..BOARD_SIZE_Y {
                if self.data[i][j].tile_owner == player && self.data[i][j].tile_type == STATE_PLAYER
                {
                    self.data[i][j].tile_type = STATE_EMPTY;
                    self.data[i][j].tile_owner = Pubkey::default();
                    msg!("Player removed");
                }
            }
        }

        Ok(())
    }

    pub fn find_player(&mut self, player: Pubkey) -> TileData {
        for i in 0..BOARD_SIZE_X {
            for j in 0..BOARD_SIZE_Y {
                if self.data[i][j].tile_owner == player && self.data[i][j].tile_type == STATE_PLAYER
                {
                    return self.data[i][j];
                }
            }
        }

        return TileData::default();
    }

    pub fn spawn_player(&mut self, player: Pubkey, lastTile: TileData) -> Result<()> {
        let mut empty_slots: Vec<(usize, usize)> = Vec::new();

        for i in 0..BOARD_SIZE_X {
            for j in 0..BOARD_SIZE_Y {
                let tile: TileData = self.data[i][j];

                if tile.tile_type == STATE_EMPTY {
                    empty_slots.push((i, j));
                } else if tile.tile_owner == player && tile.tile_type == STATE_PLAYER {
                    return Err(GameErrorCode::PlayerAlreadyExists.into());
                }
            }
        }

        if (empty_slots.len() as u64) == 0 {
            return Err(GameErrorCode::BoardIsFull.into());
        }

        let slot = Clock::get()?.slot;
        let mut rng = XorShift64 {
            a: empty_slots.len() as u64 + slot,
        };

        let random_index = (rng.next() % (empty_slots.len() as u64)) as usize;
        let random_empty_slot = empty_slots[random_index];
        msg!(
            "Chest spawn at {} {}",
            random_empty_slot.0,
            random_empty_slot.1
        );

        self.data[random_empty_slot.0][random_empty_slot.1] = lastTile;

        empty_slots.remove(random_index);

        for i in 1..=3 {
            println!("Spawn enem: {}", i);
            // Spawn enemy
            if empty_slots.len() > 0 {
                let random_index = (rng.next() % (empty_slots.len() as u64)) as usize;
                let random_empty_slot = empty_slots[random_index];
                msg!(
                    "Enemy spawn at {} {}",
                    random_empty_slot.0,
                    random_empty_slot.1
                );

                empty_slots.remove(random_index);

                self.data[random_empty_slot.0][random_empty_slot.1] = TileData {
                    tile_owner: player.clone(),
                    tile_type: STATE_ENEMY,
                    tile_health: self.floor_id + 5,
                    tile_max_health: self.floor_id + 5,
                    tile_damage: self.floor_id + 1,
                    tile_defence: self.floor_id + 1,
                    tile_armor: self.floor_id + 1,
                    tile_max_armor: self.floor_id + 1,
                    tile_level: self.floor_id + 1,
                    tile_xp: self.floor_id + 1,
                    ..Default::default()
                };
            }
        }

        if empty_slots.len() > 0 {
            let random_index = (rng.next() % (empty_slots.len() as u64)) as usize;
            let random_empty_slot = empty_slots[random_index];
            msg!(
                "Stairs spawn at {} {}",
                random_empty_slot.0,
                random_empty_slot.1
            );

            empty_slots.remove(random_index);

            self.data[random_empty_slot.0][random_empty_slot.1] = TileData {
                tile_owner: player.clone(),
                tile_type: STATE_STAIRS,
                tile_health: self.floor_id + 5,
                tile_max_health: self.floor_id + 5,
                tile_damage: self.floor_id + 1,
                tile_defence: self.floor_id + 1,
                tile_armor: self.floor_id + 1,
                tile_max_armor: self.floor_id + 1,
                tile_level: self.floor_id + 1,
                tile_xp: self.floor_id + 1,
                ..Default::default()
            };
        }

        // Spawn chest
        if empty_slots.len() > 0 {
            let random_index = (rng.next() % (empty_slots.len() as u64)) as usize;
            let random_empty_slot = empty_slots[random_index];
            msg!(
                "Player spawn at {} {}",
                random_empty_slot.0,
                random_empty_slot.1
            );

            empty_slots.remove(random_index);

            self.data[random_empty_slot.0][random_empty_slot.1] = TileData {
                tile_owner: player.clone(),
                tile_type: STATE_CHEST_GOLD,
                tile_health: 0,
                tile_damage: 0,
                tile_defence: 0,
                tile_level: self.floor_id + 1,
                tile_xp: 0,
                ..Default::default()
            };
        }

        let random_index = (rng.next() % (100)) as usize;
        msg!("Random index chest: {}", random_index);
        // Spawn Super chest
        if empty_slots.len() > 0 && random_index > 50 && self.floor_id > 0 {
            let random_index = (rng.next() % (empty_slots.len() as u64)) as usize;
            let random_empty_slot = empty_slots[random_index];
            msg!(
                "Super chest spawn at {} {}",
                random_empty_slot.0,
                random_empty_slot.1
            );

            empty_slots.remove(random_index);

            self.data[random_empty_slot.0][random_empty_slot.1] = TileData {
                tile_owner: player.clone(),
                tile_type: STATE_CHEST_BLUE,
                tile_health: 0,
                tile_damage: 0,
                tile_defence: 0,
                tile_level: 1,
                tile_xp: 0,
                ..Default::default()
            };
        }

        Ok(())
    }

    pub fn add_new_game_action(&mut self, game_action: GameAction) {
        {
            let option_add = self.id_counter.checked_add(1);
            match option_add {
                Some(val) => {
                    self.id_counter = val;
                }
                None => {
                    self.id_counter = 0;
                }
            }
        }
        self.action_index = (self.action_index + 1) % 20;
        self.game_actions[self.action_index as usize] = game_action;
    }
}

fn open_chest(
    playerData: &mut PlayerData,
    tile_datas: &mut [[TileData; BOARD_SIZE_X]; BOARD_SIZE_Y],
    player_pos_x: usize,
    player_pos_y: usize,
    chest_pos_x: usize,
    chest_pos_y: usize,
) -> Result<()> {
    // TODO: Add chest balancing
    if tile_datas[chest_pos_x][chest_pos_y].tile_type == STATE_CHEST_GOLD {
        tile_datas[player_pos_x][player_pos_y].tile_damage += 1;
        tile_datas[player_pos_x][player_pos_y].tile_defence += 1;
        tile_datas[player_pos_x][player_pos_y].tile_health += 1;
        tile_datas[player_pos_x][player_pos_y].tile_max_health += 1;
    }

    if tile_datas[chest_pos_x][chest_pos_y].tile_type == STATE_CHEST_BLUE {
        tile_datas[player_pos_x][player_pos_y].tile_damage += 1;
        tile_datas[player_pos_x][player_pos_y].tile_defence += 1;
        tile_datas[player_pos_x][player_pos_y].tile_health += 1;
        tile_datas[player_pos_x][player_pos_y].tile_max_health += 1;

        playerData.damage += 1;
        playerData.defence += 1;
        playerData.health += 1;
        playerData.max_health += 1;
    }

    move_player(
        tile_datas,
        player_pos_x,
        player_pos_y,
        chest_pos_x,
        chest_pos_y,
    )?;

    Ok(())
}

fn move_player(
    tile_datas: &mut [[TileData; BOARD_SIZE_X]; BOARD_SIZE_Y],
    player_pos_x: usize,
    player_pos_y: usize,
    new_pos_x: usize,
    new_pos_y: usize,
) -> Result<()> {
    tile_datas[new_pos_x][new_pos_y].tile_owner = tile_datas[player_pos_x][player_pos_y].tile_owner;
    tile_datas[new_pos_x][new_pos_y].tile_health =
        tile_datas[player_pos_x][player_pos_y].tile_health;
    tile_datas[new_pos_x][new_pos_y].tile_max_health =
        tile_datas[player_pos_x][player_pos_y].tile_max_health;
    tile_datas[new_pos_x][new_pos_y].tile_damage =
        tile_datas[player_pos_x][player_pos_y].tile_damage;
    tile_datas[new_pos_x][new_pos_y].tile_defence =
        tile_datas[player_pos_x][player_pos_y].tile_defence;
    tile_datas[new_pos_x][new_pos_y].tile_armor = tile_datas[player_pos_x][player_pos_y].tile_armor;
    tile_datas[new_pos_x][new_pos_y].tile_max_armor =
        tile_datas[player_pos_x][player_pos_y].tile_max_armor;
    tile_datas[new_pos_x][new_pos_y].tile_defence =
        tile_datas[player_pos_x][player_pos_y].tile_defence;
    tile_datas[new_pos_x][new_pos_y].tile_level = tile_datas[player_pos_x][player_pos_y].tile_level;
    tile_datas[new_pos_x][new_pos_y].tile_xp = tile_datas[player_pos_x][player_pos_y].tile_xp;

    msg!("Player moved to: {} {}", new_pos_x, new_pos_y);

    tile_datas[new_pos_x][new_pos_y].tile_type = STATE_PLAYER;
    tile_datas[player_pos_x][player_pos_y].tile_type = STATE_EMPTY;

    Ok(())
}

fn fight_enemy(
    playerData: &mut PlayerData,
    gameData: &mut GameData,
    player_pos_x: usize,
    player_pos_y: usize,
    enemy_x: usize,
    enemy_y: usize,
) -> Result<()> {
    gameData.data[player_pos_x][player_pos_y].tile_armor =
        gameData.data[player_pos_x][player_pos_y].tile_max_armor;
    gameData.data[enemy_x][enemy_y].tile_armor = gameData.data[enemy_x][enemy_y].tile_max_armor;

    while gameData.data[player_pos_x][player_pos_y].tile_health > 0
        && gameData.data[enemy_x][enemy_y].tile_health > 0
    {
        let player_damage = gameData.data[enemy_x][enemy_y]
            .tile_damage
            .saturating_sub(gameData.data[player_pos_x][player_pos_y].tile_defence);
        let enemy_damage = gameData.data[player_pos_x][player_pos_y]
            .tile_damage
            .saturating_sub(gameData.data[enemy_x][enemy_y].tile_defence);

        if gameData.data[player_pos_x][player_pos_y].tile_armor > 0 {
            gameData.data[player_pos_x][player_pos_y].tile_armor = gameData.data[player_pos_x]
                [player_pos_y]
                .tile_armor
                .saturating_sub(cmp::max(
                    cmp::min(
                        player_damage,
                        gameData.data[player_pos_x][player_pos_y].tile_armor,
                    ),
                    1,
                ));
        } else {
            gameData.data[player_pos_x][player_pos_y].tile_health = gameData.data[player_pos_x]
                [player_pos_y]
                .tile_health
                .saturating_sub(cmp::max(
                    cmp::min(
                        player_damage,
                        gameData.data[player_pos_x][player_pos_y].tile_health,
                    ),
                    1,
                ));
        }

        if gameData.data[enemy_x][enemy_y].tile_armor > 0 {
            gameData.data[enemy_x][enemy_y].tile_armor = gameData.data[enemy_x][enemy_y]
                .tile_armor
                .saturating_sub(cmp::max(
                    cmp::min(enemy_damage, gameData.data[enemy_x][enemy_y].tile_armor),
                    1,
                ));
        } else {
            gameData.data[enemy_x][enemy_y].tile_health = gameData.data[enemy_x][enemy_y]
                .tile_health
                .saturating_sub(cmp::max(
                    cmp::min(enemy_damage, gameData.data[enemy_x][enemy_y].tile_health),
                    1,
                ));
        }
    }

    if gameData.data[player_pos_x][player_pos_y].tile_health == 0 {
        gameData.data[player_pos_x][player_pos_y].tile_type = STATE_EMPTY;
        let new_game_action = GameAction {
            action_id: gameData.id_counter,
            action_type: ACTION_TYPE_PLAYER_DIED,
            from_x: player_pos_x as u8,
            from_y: player_pos_y as u8,
            to_x: player_pos_x as u8,
            to_y: player_pos_y as u8,
            tile: gameData.data[player_pos_x as usize][player_pos_y as usize],
            amount: 0,
        };
        playerData.current_floor = 0;
        playerData.xp = 0;
        playerData.level = 0;
        gameData.add_new_game_action(new_game_action);
        msg!("Player died");
    } else {
        msg!("Enemy killed");
        playerData.add_xp(gameData.data[enemy_x][enemy_y].tile_level + 1);

        gameData.data[player_pos_x as usize][player_pos_y as usize].tile_xp +=
            gameData.data[enemy_x][enemy_y].tile_level + 1;

        while gameData.data[player_pos_x as usize][player_pos_y as usize].tile_xp
            >= 5 * gameData.data[player_pos_x as usize][player_pos_y as usize].tile_level
        {
            gameData.data[player_pos_x][player_pos_y].tile_xp -=
                5 * gameData.data[player_pos_x][player_pos_y].tile_level;
            gameData.data[player_pos_x][player_pos_y].tile_level += 1;
            gameData.data[player_pos_x][player_pos_y].tile_max_health += 1;
            gameData.data[player_pos_x][player_pos_y].tile_health =
                gameData.data[player_pos_x][player_pos_y].tile_max_health;
            gameData.data[player_pos_x][player_pos_y].tile_damage += 1;
        }

        move_player(
            &mut gameData.data,
            player_pos_x,
            player_pos_y,
            enemy_x,
            enemy_y,
        )?;
    }

    Ok(())
}

pub struct XorShift64 {
    a: u64,
}

impl XorShift64 {
    pub fn next(&mut self) -> u64 {
        let mut x = self.a;
        x ^= x << 13;
        x ^= x >> 7;
        x ^= x << 17;
        self.a = x;
        x
    }
}
