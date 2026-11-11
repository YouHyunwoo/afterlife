"""73의 완충 게이지 규칙을 사용한 밸런싱 산술. Unity/전투 실행이 아니다."""
import argparse
import copy
import json
import math
from pathlib import Path
from calculate_hold_economy import calculate


def evaluate(config, **options):
    prepared = copy.deepcopy(config)
    for day in prepared['days']:
        day['expense'] = day['repair_hp'] / prepared['repair_per_gold'] + day['acceleration_seconds'] * prepared['acceleration_gold_per_second']
    result = calculate(prepared, initial_full=True, **options)
    # 방어 성공 판정이 아니라, 일차별로 준비할 병력의 제안 마감이다.
    minimum = [(3,0),(3,1),(3,3),(6,3),(6,6),(6,6),(9,9),(9,9),(12,12),(12,12)]
    result['preparation_issues'] = []
    for row, day in zip(result['rows'], prepared['days']):
        power = row['construction_work']
        row['planned_repair_work_count'] = math.ceil(day['repair_hp'] / (prepared['repair_per_work'] * power))
        row['planned_repair_seconds_upper'] = row['planned_repair_work_count'] * prepared['work_interval']
        row['planned_acceleration_seconds'] = day['acceleration_seconds']
        warriors, archers = minimum[row['day'] - 1]
        row['night_ready_minimum'] = {'warrior':warriors,'archer':archers}
        if row['ready_at_night']['warrior'] < warriors or row['ready_at_night']['archer'] < archers:
            result['preparation_issues'].append(f'{row["day"]}일 준비 목표 미달')
        # 복구 시간/가속 효과는 경제 코어에서 실행하지 않는 목표값이다.
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--config', type=Path, default=Path(__file__).with_name('gauge-economy.json'))
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    config = json.loads(args.config.read_text(encoding='utf-8'))
    variants = {'reference':{}, 'less_harvest_time':{'harvest_scale':0.8},
        'farm_destroyed_day5':{'destroy_farm':True}, 'farm_adjacency':{'farm_bonus':1.5},
        'later_fertile_farm_upper_bound':{'fertile_farms':1,'fertile_start_ordinal':2},
        'fertile_and_adjacency_upper_bound':{'farm_bonus':1.5,'fertile_farms':1,'fertile_start_ordinal':2},
        'extra_first_farm':{'extra_first_farm':True}}
    result = {name:evaluate(config, **options) for name, options in variants.items()}
    recovery = copy.deepcopy(config)
    recovery['days'][5]['build'] = ['farm', 'farm']
    recovery['days'][5]['acceleration_seconds'] = 10
    recovery['days'][6]['build'] = ['barracks', 'archery']
    result['farm_recovery_priority'] = evaluate(recovery, destroy_farm=True)
    relaxed = copy.deepcopy(config)
    relaxed['days'][2]['build'] = ['altar']
    relaxed['days'][3]['build'] = ['farm', 'barracks']
    relaxed['days'][5]['build'] = ['farm', 'barracks']
    relaxed['days'][7]['build'] = ['tower', 'house', 'house', 'house']
    result['less_harvest_adjusted_order'] = evaluate(relaxed, harvest_scale=0.8)
    old_charge = copy.deepcopy(config)
    for day in old_charge['days']:
        day['expense'] = day['repair_hp'] / old_charge['repair_per_gold'] + day['acceleration_seconds']
    result['only_first_charge_empty'] = calculate(old_charge)
    actual = copy.deepcopy(config)
    actual['starting_gold'] = 0
    actual['harvest_base'] = 2
    actual['farm_seconds'] = 3
    for name, cost in {'farm':30,'barracks':80,'archery':120,'altar':120,'house':20,'tower':95}.items():
        actual['buildings'][name]['cost'] = cost
    for day in actual['days']:
        day.update(research_cost=0,harvest_yield=2,construction_work=1,farm_seconds=3)
    actual['acceleration_gold_per_second'] = 5
    result['current_values_in_ten_day_plan'] = evaluate(actual)
    encoded = json.dumps(result, ensure_ascii=False, indent=2) + '\n'
    if args.output:
        args.output.write_text(encoded, encoding='utf-8')
    else:
        print(encoded)


if __name__ == '__main__':
    main()
