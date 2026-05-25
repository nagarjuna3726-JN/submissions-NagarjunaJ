import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { SearchFormComponent } from './search-form.component';

describe('SearchFormComponent', () => {
  let component: SearchFormComponent;
  let fixture: ComponentFixture<SearchFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SearchFormComponent ],
      imports: [ ReactiveFormsModule ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SearchFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  describe('Component Initialization', () => {
    it('should create search form component', () => {
      expect(component).toBeTruthy();
    });

    it('should initialize form with required controls', () => {
      expect(component.searchForm.get('destination')).toBeTruthy();
      expect(component.searchForm.get('checkIn')).toBeTruthy();
      expect(component.searchForm.get('checkOut')).toBeTruthy();
      expect(component.searchForm.get('roomType')).toBeTruthy();
    });

    it('should populate destination lists (domestic + international)', () => {
      expect(component.domesticDestinations.length).toBeGreaterThan(0);
      expect(component.internationalDestinations.length).toBeGreaterThan(0);
      expect(component.roomTypes).toContain('Standard');
      expect(component.roomTypes).toContain('Deluxe');
      expect(component.roomTypes).toContain('Suite');
    });

    it('should initialize form as invalid (all fields empty)', () => {
      expect(component.searchForm.valid).toBeFalsy();
    });
  });

  describe('Form Validation - Required Fields', () => {
    it('should mark destination as invalid when empty', () => {
      const destination = component.searchForm.get('destination');
      destination?.setValue('');
      expect(destination?.hasError('required')).toBeTruthy();
    });

    it('should mark checkIn as invalid when empty', () => {
      const checkIn = component.searchForm.get('checkIn');
      checkIn?.setValue('');
      expect(checkIn?.hasError('required')).toBeTruthy();
    });

    it('should mark checkOut as invalid when empty', () => {
      const checkOut = component.searchForm.get('checkOut');
      checkOut?.setValue('');
      expect(checkOut?.hasError('required')).toBeTruthy();
    });

    it('should allow roomType to be empty (optional)', () => {
      const roomType = component.searchForm.get('roomType');
      roomType?.setValue('');
      expect(roomType?.hasError('required')).toBeFalsy();
    });
  });

  describe('Form Validation - Cross-Field Validator', () => {
    beforeEach(() => {
      component.searchForm.get('destination')?.setValue('London');
    });

    it('should mark form as invalid if checkOut is before checkIn', () => {
      component.searchForm.get('checkIn')?.setValue('2025-06-10');
      component.searchForm.get('checkOut')?.setValue('2025-06-05');
      expect(component.searchForm.hasError('checkOutBeforeCheckIn')).toBeTruthy();
    });

    it('should mark form as invalid if checkOut equals checkIn', () => {
      component.searchForm.get('checkIn')?.setValue('2025-06-10');
      component.searchForm.get('checkOut')?.setValue('2025-06-10');
      expect(component.searchForm.hasError('checkOutBeforeCheckIn')).toBeTruthy();
    });

    it('should mark form as valid if checkOut is after checkIn', () => {
      component.searchForm.get('checkIn')?.setValue('2025-06-10');
      component.searchForm.get('checkOut')?.setValue('2025-06-12');
      expect(component.searchForm.hasError('checkOutBeforeCheckIn')).toBeFalsy();
    });

    it('should make form valid when all fields correct', () => {
      component.searchForm.get('checkIn')?.setValue('2025-06-10');
      component.searchForm.get('checkOut')?.setValue('2025-06-12');
      component.searchForm.get('roomType')?.setValue('Deluxe');
      expect(component.searchForm.valid).toBeTruthy();
    });
  });

  describe('Form Submission', () => {
    it('should emit onSearch with valid criteria', (done) => {
      const criteria = {
        destination: 'London',
        checkIn: '2025-06-10',
        checkOut: '2025-06-12',
        roomType: 'Deluxe'
      };

      component.onSearch.subscribe((result) => {
        expect(result.destination).toBe('London');
        expect(result.checkIn).toBe('2025-06-10');
        expect(result.checkOut).toBe('2025-06-12');
        expect(result.roomType).toBe('Deluxe');
        done();
      });

      component.searchForm.patchValue(criteria);
      component.submit();
    });

    it('should not emit onSearch if form is invalid', (done) => {
      let emitted = false;
      component.onSearch.subscribe(() => {
        emitted = true;
      });

      component.searchForm.patchValue({
        destination: 'London',
        checkIn: '2025-06-10',
        checkOut: '2025-06-05' // Invalid: before checkIn
      });

      component.submit();

      setTimeout(() => {
        expect(emitted).toBeFalsy();
        done();
      }, 100);
    });

    it('should emit with undefined roomType when empty (optional field)', (done) => {
      const criteria = {
        destination: 'New York',
        checkIn: '2025-06-10',
        checkOut: '2025-06-12',
        roomType: ''
      };

      component.onSearch.subscribe((result) => {
        expect(result.roomType).toBeUndefined();
        done();
      });

      component.searchForm.patchValue(criteria);
      component.submit();
    });
  });

  describe('Form Reset', () => {
    it('should clear all form fields when reset called', () => {
      component.searchForm.patchValue({
        destination: 'London',
        checkIn: '2025-06-10',
        checkOut: '2025-06-12',
        roomType: 'Deluxe'
      });

      component.clear();

      expect(component.searchForm.get('destination')?.value).toBeNull();
      expect(component.searchForm.get('checkIn')?.value).toBeNull();
      expect(component.searchForm.get('checkOut')?.value).toBeNull();
      expect(component.searchForm.get('roomType')?.value).toBeNull();
    });
  });

  describe('UI Helper Properties', () => {
    it('should combine domestic and international destinations in allDestinations', () => {
      const all = component.allDestinations;
      expect(all).toContain('New York');
      expect(all).toContain('London');
      expect(all.length).toBe(
        component.domesticDestinations.length + component.internationalDestinations.length
      );
    });

    it('should set isSubmitDisabled true when form invalid', () => {
      component.searchForm.get('destination')?.setValue('');
      expect(component.isSubmitDisabled).toBeTruthy();
    });

    it('should set isSubmitDisabled false when form valid', () => {
      component.searchForm.patchValue({
        destination: 'London',
        checkIn: '2025-06-10',
        checkOut: '2025-06-12'
      });
      expect(component.isSubmitDisabled).toBeFalsy();
    });
  });
});
