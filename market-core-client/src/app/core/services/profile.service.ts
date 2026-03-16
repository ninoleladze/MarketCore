import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProfileDto, UpdateProfileCommand, ChangePasswordCommand } from '../models/profile.models';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly url = `${environment.apiUrl}/Profile`;

  constructor(private http: HttpClient) {}

  getProfile(): Observable<ProfileDto> {
    return this.http.get<ProfileDto>(this.url);
  }

  updateProfile(command: UpdateProfileCommand): Observable<ProfileDto> {
    return this.http.put<ProfileDto>(this.url, command);
  }

  changePassword(command: ChangePasswordCommand): Observable<void> {
    return this.http.put<void>(`${this.url}/password`, command);
  }
}
