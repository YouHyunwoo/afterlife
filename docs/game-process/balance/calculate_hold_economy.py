"""지정된 작업 시간과 시설 순서를 계산하는 오프라인 경제 도구. 게임 실행은 아니다."""
import argparse
import copy
import json
import math
from pathlib import Path


def calculate(config, interval_scale=1, farm_bonus=1, destroy_farm=False,
              extra_first_farm=False, initial_full=False, harvest_scale=1,
              fertile_farms=0, fertile_start_ordinal=1):
    dt, gold, etheron = 0.25, float(config['starting_gold']), 0
    remainder, sites, rows, issues = 0.0, [], [], []
    harvest_yield, construction_work, farm_seconds = config.get('harvest_base', 1), 1, config['farm_seconds']
    interval = config['work_interval'] * interval_scale
    cooldown, game_time, total_time = (0 if initial_full else interval), 0.0, 0.0
    farm_ordinal = 0
    for index, day in enumerate(config['days'], 1):
        start_gold, start_etheron = gold, etheron
        cost = day.get('research_cost', 0)
        if etheron >= cost:
            etheron -= cost
            harvest_yield = day.get('harvest_yield', harvest_yield)
            construction_work = day.get('construction_work', construction_work)
            farm_seconds = day.get('farm_seconds', farm_seconds)
        else:
            issues.append(f'{index}일 강화 재화 부족: {etheron}/{cost}E')
        purchases = list(day['build'])
        if extra_first_farm and index == 1:
            purchases.insert(1, 'farm')
        cursor, blocked_until, harvest_time, construction_time = 0, 0, 0, 0
        direct_gold, farm_gold, spent, expense = 0, 0, 0, 0
        events, snapshot = [], {}
        end_time = day['low'] + 10 + day['night']
        for step in range(0 if initial_full and index == 1 else 1, round(end_time / dt) + 1):
            t = step * dt
            elapsed = 0 if step == 0 else dt
            game_time += elapsed
            cooldown = max(0, cooldown - elapsed)
            unfinished = next((s for s in sites if s['progress'] < 1), None)
            if t <= day['low'] + 10 and t >= blocked_until and unfinished is None and cursor < len(purchases):
                name = purchases[cursor]
                spec = config['buildings'][name]
                if gold >= spec['cost']:
                    gold -= spec['cost']
                    spent += spec['cost']
                    if name == 'farm':
                        farm_ordinal += 1
                    sites.append(dict(name=name, spec=spec, progress=0, cycle=0, units=[],
                        completed_at=None, yield_multiplier=2 if name == 'farm' and
                        fertile_start_ordinal <= farm_ordinal < fertile_start_ordinal + fertile_farms else 1))
                    events.append(dict(kind='purchase', building=name, time=t))
                    cursor += 1
                    blocked_until = t + config['placement_seconds']
            for site in sites:
                if site['progress'] < 1:
                    site['progress'] += config['auto_construction'] * config['local_construction_multiplier'] * elapsed / site['spec']['construction']
                    if site['progress'] >= 1:
                        site['progress'], site['completed_at'] = 1, game_time
                        events.append(dict(kind='complete', building=site['name'], time=t))
            target = next((s for s in sites if s['progress'] < 1), None)
            if t >= blocked_until and t <= day['low'] + 10 and target:
                construction_time += elapsed
                if cooldown <= 1e-9:
                    target['progress'] += construction_work * config['local_construction_multiplier'] / target['spec']['construction']
                    cooldown = interval
                    if target['progress'] >= 1:
                        target['progress'], target['completed_at'] = 1, game_time
                        events.append(dict(kind='complete', building=target['name'], time=t))
            elif t >= blocked_until and t <= day['low'] and harvest_time + elapsed <= day['harvest_seconds'] * harvest_scale + 1e-9:
                harvest_time += elapsed
                if cooldown <= 1e-9:
                    remainder += harvest_yield
                    amount = math.floor(remainder + 1e-9)
                    remainder -= amount
                    gold += amount
                    direct_gold += amount
                    cooldown = interval
            for site in sites:
                spec = site['spec']
                if site['progress'] < 1 or 'product' not in spec or site['completed_at'] >= game_time:
                    continue
                product = spec['product']
                if product in ('warrior', 'archer') and len(site['units']) >= 3:
                    site['cycle'] = 0
                    continue
                farm_count = sum(s['name'] == 'farm' and s['progress'] >= 1 for s in sites)
                adjacency = 1 + min(farm_bonus - 1, 0.25 * max(0, farm_count - 1))
                seconds = farm_seconds / adjacency if product == 'gold' else spec['cycle']
                site['cycle'] += elapsed / seconds
                while site['cycle'] >= 1 - 1e-9:
                    site['cycle'] -= 1
                    if product == 'gold':
                        gold += site['yield_multiplier']
                        farm_gold += site['yield_multiplier']
                    elif product == 'etheron':
                        etheron += 1
                    else:
                        site['units'].append(game_time + config['troop_walk_seconds'])
                        if len(site['units']) >= 3:
                            site['cycle'] = 0
                            break
            if abs(t - day['low'] - 10) < 1e-9:
                snapshot = {p:sum(sum(u <= game_time for u in s['units']) for s in sites if s['spec'].get('product') == p) for p in ('warrior', 'archer')}
            if abs(t - day['low'] - 20) < 1e-9:
                if gold < day['expense']:
                    issues.append(f'{index}일 복구·가속비 부족: {gold:.1f}/{day["expense"]}G')
                expense = min(gold, day['expense'])
                gold -= expense
                # 고정 손실 입력을 시설에 번갈아 배분. 전투 AI나 생존 판정은 없다.
                for product, count in day.get('losses', {}).items():
                    owners = [s for s in sites if s['spec'].get('product') == product]
                    for _ in range(count):
                        owner = max(owners, key=lambda s:len(s['units']), default=None)
                        if owner and owner['units']:
                            owner['units'].pop(0)
                if destroy_farm and index == 5:
                    victim = next((s for s in sites if s['name'] == 'farm'), None)
                    if victim:
                        sites.remove(victim)
                        events.append(dict(kind='destroy', building='farm', time=t))
        if cursor < len(purchases):
            issues.append(f'{index}일 미구매: {purchases[cursor:]}')
        if any(s['progress'] < 1 for s in sites):
            issues.append(f'{index}일 정산 시 미완공 시설')
        rows.append(dict(day=index, start_gold=start_gold, direct_gold=direct_gold, farm_gold=farm_gold,
            building_spend=spent, expense=expense, end_gold=round(gold, 2), start_etheron=start_etheron,
            end_etheron=etheron, harvest_seconds=harvest_time, construction_seconds=construction_time,
            requested_expense=day['expense'], expense_shortfall=day['expense']-expense,
            ready_at_night=snapshot, events=events, construction_work=construction_work,
            harvest_yield=harvest_yield, farm_seconds=farm_seconds))
        total_time += end_time + day['menu']
    return dict(total_seconds=total_time, rows=rows, issues=issues)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--config', type=Path, default=Path(__file__).with_name('hold-economy.json'))
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    config = json.loads(args.config.read_text(encoding='utf-8'))
    variants = {'reference':{}, 'slower_work':{'interval_scale':1.25},
        'farm_destroyed_day5':{'destroy_farm':True}, 'farm_adjacency_bonus':{'farm_bonus':1.5},
        'extra_first_farm':{'extra_first_farm':True}}
    result = {name:calculate(config, **options) for name, options in variants.items()}
    recovery = copy.deepcopy(config)
    recovery['days'][5]['build'] = ['farm', 'farm']
    recovery['days'][5]['expense'] = 40
    recovery['days'][6]['build'] = ['barracks', 'archery']
    result['farm_recovery_priority'] = calculate(recovery, destroy_farm=True)
    encoded = json.dumps(result, ensure_ascii=False, indent=2)
    if args.output:
        args.output.write_text(encoded + '\n', encoding='utf-8')
    else:
        print(encoded)


if __name__ == '__main__':
    main()
